using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;

// 맵 렌더링 최적화 전후를 "같은 시점, 같은 조건"에서 재기 위한 벤치마크.
// 에디터 메뉴(Tools/Map Optimization)가 플레이 모드를 켤 때만 동작하고, 빌드에는 아무 영향이 없다.
public class CullingBenchmarkRunner : MonoBehaviour
{
#if UNITY_EDITOR
    public const string PendingKey = "MapOpt.BenchPendingLabel";
    public const string DoneKey = "MapOpt.BenchDone";
    public const string MapStaticPath = "Map/Map_v1/Static";

    public static string ReportDir => Path.Combine(Application.dataPath, "OptimizationReport~");

    public static void Log(string msg)
    {
        try
        {
            Directory.CreateDirectory(ReportDir);
            File.AppendAllText(Path.Combine(ReportDir, "log.txt"), $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        }
        catch (Exception) { }
        Debug.Log("[MapOpt] " + msg);
    }

    private const int StartupFrames = 90;  // 정적 배칭, 셰이더 로딩이 끝날 때까지 기다림
    private const int WarmFrames = 8;      // 시점을 옮긴 뒤 LOD, 컬링이 안정될 때까지
    private const int SampleFrames = 20;   // 평균을 낼 프레임 수

    private static readonly string[] CounterNames =
    {
        "SetPass Calls Count", "Draw Calls Count", "Batches Count", "Total Batches Count",
        "Triangles Count", "Vertices Count", "Shadow Casters Count",
        "Static Batched Draw Calls Count", "Static Batches Count",
    };

    private static readonly string[] TimerNames =
    {
        "Main Thread", "Render Thread", "PlayerLoop", "CullScriptable", "Culling",
        "SceneCulling", "CullResults.CreateSharedRendererScene", "Inl_ScriptableRenderer.Setup",
    };

    private static readonly string[] StatsPropNames =
    {
        "batches", "drawCalls", "setPassCalls", "triangles", "vertices", "shadowCasters",
    };

    private struct View
    {
        public string Name;
        public Vector3 Pos;
        public Quaternion Rot;
        public bool Overview;
    }

    private string _label;
    private Camera _cam;
    private readonly List<string> _metricNames = new List<string>();
    private readonly List<Func<double>> _metricReaders = new List<Func<double>>();
    private readonly List<ProfilerRecorder> _recorders = new List<ProfilerRecorder>();
    private int _errorCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        string label = UnityEditor.SessionState.GetString(PendingKey, "");
        if (string.IsNullOrEmpty(label))
        {
            return;
        }
        UnityEditor.SessionState.EraseString(PendingKey);
        Application.runInBackground = true;

        var go = new GameObject("__CullingBenchmark");
        var runner = go.AddComponent<CullingBenchmarkRunner>();
        runner._label = label;
        runner.PrepareScene();
    }

    // 다른 스크립트의 Start보다 먼저 불려서, 측정을 흔드는 요소를 끈다.
    private void PrepareScene()
    {
        Application.logMessageReceived += OnLog;
        Log($"[{_label}] 벤치마크 준비");

        _cam = Camera.main;
        if (_cam == null)
        {
            Log("Main Camera를 찾지 못함");
            return;
        }

        Assembly gameAssembly = typeof(CullingBenchmarkRunner).Assembly;

        // 카메라를 움직이는 게임 스크립트(CameraFollow, CameraRotate 등)를 끈다. URP 카메라 데이터는 그대로 둔다.
        foreach (var mb in _cam.GetComponents<MonoBehaviour>())
        {
            if (mb.GetType().Assembly == gameAssembly)
            {
                mb.enabled = false;
            }
        }

        // 좀비는 무작위 위치에 생겨서 측정이 매번 달라지므로 끈다.
        foreach (var spawner in FindObjectsByType<ZombieSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            spawner.enabled = false;
        }
        foreach (var zombie in FindObjectsByType<Zombie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            zombie.gameObject.SetActive(false);
        }

        // 플레이어 모델은 스폰 지점에 서 있으므로 일부 시점에만 찍힌다. 렌더러만 끈다.
        foreach (var player in FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (var r in player.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = false;
            }
        }

        // 맵 렌더링만 재기 위해 미니맵 카메라와 UI를 끈다.
        foreach (var cam in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cam != _cam)
            {
                cam.enabled = false;
            }
        }
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            canvas.enabled = false;
        }

        StartCoroutine(Run());
    }

    private void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
        {
            _errorCount++;
            if (_errorCount <= 20)
            {
                try
                {
                    File.AppendAllText(Path.Combine(ReportDir, "log.txt"),
                        $"[{DateTime.Now:HH:mm:ss}] (play {type}) {condition}\n{stackTrace}\n");
                }
                catch (Exception) { }
            }
        }
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        foreach (var r in _recorders)
        {
            r.Dispose();
        }
    }

    private IEnumerator Run()
    {
        for (int i = 0; i < StartupFrames; i++)
        {
            yield return null;
        }

        SetupMetrics();
        List<View> views = BuildViews();
        Log($"[{_label}] 시점 {views.Count}개, 지표 {_metricNames.Count}개: {string.Join(", ", _metricNames)}");

        var csv = new StringBuilder();
        csv.Append("label,occlusion,view,overview,x,y,z,yaw,pitch");
        foreach (var n in _metricNames)
        {
            csv.Append(',').Append(n.Replace(',', ' '));
        }
        csv.AppendLine();

        // 모드별(오클루전 끔/켬) 시점 평균을 모은다.
        var perMode = new Dictionary<bool, List<double[]>>
        {
            { false, new List<double[]>() },
            { true, new List<double[]>() },
        };
        var perModeOverview = new Dictionary<bool, List<double[]>>
        {
            { false, new List<double[]>() },
            { true, new List<double[]>() },
        };

        foreach (bool occlusion in new[] { false, true })
        {
            _cam.useOcclusionCulling = occlusion;
            foreach (var v in views)
            {
                _cam.transform.SetPositionAndRotation(v.Pos, v.Rot);
                for (int i = 0; i < WarmFrames; i++)
                {
                    yield return null;
                }

                var sums = new double[_metricReaders.Count];
                for (int f = 0; f < SampleFrames; f++)
                {
                    yield return null;
                    for (int k = 0; k < _metricReaders.Count; k++)
                    {
                        sums[k] += _metricReaders[k]();
                    }
                }
                for (int k = 0; k < sums.Length; k++)
                {
                    sums[k] /= SampleFrames;
                }

                (v.Overview ? perModeOverview : perMode)[occlusion].Add(sums);

                Vector3 e = v.Rot.eulerAngles;
                csv.Append(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4:F1},{5:F1},{6:F1},{7:F0},{8:F0}",
                    _label, occlusion ? "on" : "off", v.Name, v.Overview ? 1 : 0, v.Pos.x, v.Pos.y, v.Pos.z, e.y, e.x));
                foreach (double s in sums)
                {
                    csv.Append(',').Append(s.ToString("F3", CultureInfo.InvariantCulture));
                }
                csv.AppendLine();
            }
        }

        File.WriteAllText(Path.Combine(ReportDir, $"bench_{_label}.csv"), csv.ToString());
        WriteSummary(perMode, perModeOverview);

        Log($"[{_label}] 벤치마크 끝 (에러 {_errorCount}개)");
        UnityEditor.SessionState.SetBool(DoneKey, true);
        UnityEditor.EditorApplication.ExitPlaymode();
    }

    private void WriteSummary(Dictionary<bool, List<double[]>> ground, Dictionary<bool, List<double[]>> overview)
    {
        string path = Path.Combine(ReportDir, "summary.csv");
        bool newFile = !File.Exists(path);
        var sb = new StringBuilder();
        if (newFile)
        {
            sb.Append("label,occlusion,subset,views");
            foreach (var n in _metricNames)
            {
                sb.Append(',').Append(n.Replace(',', ' '));
            }
            sb.AppendLine();
        }

        foreach (bool occ in new[] { false, true })
        {
            foreach (var (subset, rows) in new[] { ("ground", ground[occ]), ("overview", overview[occ]) })
            {
                if (rows.Count == 0)
                {
                    continue;
                }
                sb.Append($"{_label},{(occ ? "on" : "off")},{subset},{rows.Count}");
                for (int k = 0; k < _metricNames.Count; k++)
                {
                    double mean = rows.Average(r => r[k]);
                    sb.Append(',').Append(mean.ToString("F3", CultureInfo.InvariantCulture));
                }
                sb.AppendLine();
            }
        }
        File.AppendAllText(path, sb.ToString());

        Log($"[{_label}] 환경: {Screen.width}x{Screen.height}, FOV {_cam.fieldOfView:F1}, Far {_cam.farClipPlane}, LOD Bias {QualitySettings.lodBias}, Quality {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
    }

    private void SetupMetrics()
    {
        // 사용할 수 있는 프로파일러 카운터/마커를 이름으로 찾는다.
        var handles = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(handles);
        var byName = new Dictionary<string, ProfilerRecorderDescription>();
        var cullNames = new List<string>();
        foreach (var h in handles)
        {
            var d = ProfilerRecorderHandle.GetDescription(h);
            if (!byName.ContainsKey(d.Name))
            {
                byName[d.Name] = d;
            }
            if (d.Name.IndexOf("cull", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cullNames.Add($"{d.Name}({d.Category.Name})");
            }
        }
        Log($"[{_label}] 'cull'이 들어간 마커: {string.Join(" | ", cullNames.Take(40))}");

        foreach (var name in CounterNames)
        {
            if (byName.TryGetValue(name, out var d))
            {
                var rec = ProfilerRecorder.StartNew(d.Category, d.Name);
                _recorders.Add(rec);
                int idx = _recorders.Count - 1;
                _metricNames.Add(name.Replace(" Count", ""));
                _metricReaders.Add(() => _recorders[idx].LastValue);
            }
        }
        foreach (var name in TimerNames)
        {
            if (byName.TryGetValue(name, out var d))
            {
                var rec = ProfilerRecorder.StartNew(d.Category, d.Name);
                _recorders.Add(rec);
                int idx = _recorders.Count - 1;
                _metricNames.Add(name + " ms");
                _metricReaders.Add(() => _recorders[idx].LastValue / 1e6);
            }
        }

        // Game 뷰 Stats 창과 같은 값(UnityStats)도 함께 기록한다.
        Type statsType = Type.GetType("UnityEditor.UnityStats, UnityEditor");
        if (statsType != null)
        {
            foreach (var p in StatsPropNames)
            {
                PropertyInfo prop = statsType.GetProperty(p, BindingFlags.Public | BindingFlags.Static);
                if (prop != null)
                {
                    _metricNames.Add("Stats." + p);
                    _metricReaders.Add(() => Convert.ToDouble(prop.GetValue(null)));
                }
            }
        }

        _metricNames.Add("Frame ms");
        _metricReaders.Add(() => Time.unscaledDeltaTime * 1000.0);
    }

    // 고정 시점: 시작 카메라 시점 + 네비메시 위 격자 지점(눈높이, 4방향) + 높은 조망 2곳.
    private List<View> BuildViews()
    {
        var views = new List<View>();
        views.Add(new View { Name = "start", Pos = _cam.transform.position, Rot = _cam.transform.rotation });

        GameObject root = GameObject.Find(MapStaticPath);
        if (root == null)
        {
            Log("Map/Map_v1/Static을 찾지 못함 - 시작 시점만 측정");
            return views;
        }

        Bounds b = default;
        bool has = false;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            if (!has)
            {
                b = r.bounds;
                has = true;
            }
            else
            {
                b.Encapsulate(r.bounds);
            }
        }
        Log($"[{_label}] 맵 범위 center {b.center}, size {b.size}");

        const int grid = 5;
        var points = new List<Vector3>();
        for (int i = 0; i < grid; i++)
        {
            for (int j = 0; j < grid; j++)
            {
                float tx = Mathf.Lerp(0.1f, 0.9f, i / (float)(grid - 1));
                float tz = Mathf.Lerp(0.1f, 0.9f, j / (float)(grid - 1));
                var probe = new Vector3(Mathf.Lerp(b.min.x, b.max.x, tx), b.min.y + 1f, Mathf.Lerp(b.min.z, b.max.z, tz));
                if (NavMesh.SamplePosition(probe, out NavMeshHit hit, 25f, NavMesh.AllAreas))
                {
                    if (points.All(p => (p - hit.position).sqrMagnitude > 9f))
                    {
                        points.Add(hit.position);
                    }
                }
            }
        }

        int idx = 0;
        foreach (var p in points)
        {
            for (int yaw = 0; yaw < 360; yaw += 90)
            {
                views.Add(new View
                {
                    Name = $"g{idx:D2}_y{yaw}",
                    Pos = p + Vector3.up * 1.6f,
                    Rot = Quaternion.Euler(5f, yaw, 0f),
                });
            }
            idx++;
        }

        float h = b.max.y + 40f;
        foreach (var corner in new[] { new Vector3(b.min.x, h, b.min.z), new Vector3(b.max.x, h, b.max.z) })
        {
            views.Add(new View
            {
                Name = corner.x < b.center.x ? "overview_min" : "overview_max",
                Pos = corner,
                Rot = Quaternion.LookRotation(b.center - corner),
                Overview = true,
            });
        }
        return views;
    }
#endif
}
