using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

// GameScene의 Map/Map_v1/Static을 대상으로 오클루전 컬링, LOD Group, Mesh LOD를 적용하고
// 각 단계마다 CullingBenchmarkRunner로 같은 시점을 측정한다. 결과는 Assets/OptimizationReport~ 에 저장된다.
[InitializeOnLoad]
public static class MapOptimizationTools
{
    private const string StepKey = "MapOpt.PipelineStep";
    private const string RestoreMaxKey = "MapOpt.RestoreMaximize";
    private const string RunInBgOrigKey = "MapOpt.RunInBackgroundOrig";
    private const string RunInBgSetKey = "MapOpt.RunInBackgroundSet";
    private const string Menu = "Tools/Map Optimization/";

    public enum Category { Ground, SmallProp, Container, MeshLodSet, Large }

    static MapOptimizationTools()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += PollCommandFile;
    }

    // 메뉴를 누르지 않고도 실행할 수 있게, Assets/OptimizationReport~/command.txt에 적힌 명령을 처리한다.
    private static double _nextPoll;

    private static void PollCommandFile()
    {
        if (EditorApplication.timeSinceStartup < _nextPoll || EditorApplication.isPlayingOrWillChangePlaymode
            || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }
        _nextPoll = EditorApplication.timeSinceStartup + 2.0;

        string path = Path.Combine(CullingBenchmarkRunner.ReportDir, "command.txt");
        if (!File.Exists(path))
        {
            return;
        }
        string command;
        try
        {
            command = File.ReadAllText(path).Trim();
            File.Delete(path);
        }
        catch (Exception)
        {
            return;
        }

        Log($"명령 파일 실행: {command}");
        try
        {
            string[] parts = command.Split(' ');
            switch (parts[0])
            {
                case "resume": ResumeFrom(int.Parse(parts[1])); break;
                case "bench": StartBench(parts[1]); break;
                case "meshlod": SetMeshLodImport(parts[1] == "on"); break;
                case "batching": SetBatchingStatic(Category.MeshLodSet, parts[1] == "on"); SaveScene(); break;
                case "bake": BakeOcclusion(); SaveScene(); break;
                case "dump": DumpClassification(); break;
                case "stop": StopPipeline(); break;
                default: Log("알 수 없는 명령"); break;
            }
        }
        catch (Exception e)
        {
            Log($"명령 실행 중 예외: {e}");
        }
    }

    private static void Log(string msg) => CullingBenchmarkRunner.Log(msg);

    // ───────── 메뉴 ─────────

    [MenuItem(Menu + "Run Full Pipeline")]
    public static void RunFullPipeline()
    {
        if (!CheckScene())
        {
            return;
        }
        Log("===== 전체 파이프라인 시작 =====");
        SessionState.SetInt(StepKey, 0);
        RunStep(0);
    }

    [MenuItem(Menu + "Bench Only (manual)")]
    public static void BenchOnly() => StartBench("manual_" + DateTime.Now.ToString("HHmmss"));

    // 도메인 리로드 등으로 파이프라인이 멈췄을 때 저장된 단계부터 다시 시작한다.
    [MenuItem(Menu + "Continue Pipeline")]
    public static void ContinuePipeline()
    {
        int step = SessionState.GetInt(StepKey, -1);
        if (step < 0)
        {
            Log("이어서 할 파이프라인 단계가 없음");
            return;
        }
        Log($"파이프라인 {step}단계부터 이어서 진행");
        RunStep(step);
    }

    [MenuItem(Menu + "Resume From/2 LOD Groups")]
    public static void ResumeFrom2() => ResumeFrom(2);

    [MenuItem(Menu + "Resume From/3 Mesh LOD")]
    public static void ResumeFrom3() => ResumeFrom(3);

    [MenuItem(Menu + "Resume From/4 Batching Off")]
    public static void ResumeFrom4() => ResumeFrom(4);

    private static void ResumeFrom(int step)
    {
        if (!CheckScene())
        {
            return;
        }
        Log($"파이프라인 {step}단계부터 재개");
        SessionState.SetInt(StepKey, step);
        RunStep(step);
    }

    // Mesh LOD 실험을 되돌리고(정적 배칭 복구, 임포트 끔), LOD Group 전환 거리를 LOD Bias에 맞게 다시 잡은 뒤 최종 측정한다.
    [MenuItem(Menu + "Finalize (Revert Mesh LOD + Retune LOD + Bench)")]
    public static void FinalizeOptimization()
    {
        if (!CheckScene())
        {
            return;
        }
        try
        {
            SetBatchingStatic(Category.MeshLodSet, true);
            SetMeshLodImport(false);
            RetuneLodGroups();
            BakeOcclusion();
            SaveScene();
            SessionState.SetInt(StepKey, -1);
            StartBench("05_final");
        }
        catch (Exception e)
        {
            Log($"Finalize 예외: {e}");
        }
    }

    // LOD Group 전환 높이는 화면 비율 기준이라 Quality의 LOD Bias가 곱해진다. 의도한 거리(m)가 되도록 Bias를 반영해 다시 계산한다.
    public static void RetuneLodGroups()
    {
        float bias = QualitySettings.lodBias;
        float tan = Mathf.Tan(30f * Mathf.Deg2Rad);
        int count = 0;
        foreach (var unit in GetUnits())
        {
            var group = unit.GetComponent<LODGroup>();
            if (group == null) continue;
            float size = group.size;
            LOD[] lods = group.GetLODs();
            if (lods.Length == 2)
            {
                lods[0].screenRelativeTransitionHeight = HeightFor(size, 40f, bias, tan);   // 40m부터 저사양 메시
                lods[1].screenRelativeTransitionHeight = HeightFor(size, 150f, bias, tan);  // 150m 밖은 그리지 않음
            }
            else if (lods.Length == 1)
            {
                float distance = size < 1.5f ? 60f : size < 3f ? 90f : 120f;
                lods[0].screenRelativeTransitionHeight = HeightFor(size, distance, bias, tan);
            }
            group.SetLODs(lods);
            count++;
        }
        Log($"LOD Group {count}개 전환 거리 재설정 (LOD Bias {bias}: 소품 60/90/120m 컬링, 컨테이너 40m LOD1, 150m 컬링)");
    }

    private static float HeightFor(float size, float distance, float bias, float tan)
    {
        return Mathf.Clamp(size * bias / (2f * distance * tan), 0.002f, 0.9f);
    }

    [MenuItem(Menu + "Stop Pipeline")]
    public static void StopPipeline()
    {
        SessionState.SetInt(StepKey, -1);
        Log("파이프라인 중단");
    }

    [MenuItem(Menu + "Step/1 Occlusion Flags + Bake")]
    public static void MenuOcclusion() { ApplyOcclusionFlags(); BakeOcclusion(); SaveScene(); }

    [MenuItem(Menu + "Step/2 LOD Groups + Bake")]
    public static void MenuLod() { AddLodGroups(); BakeOcclusion(); SaveScene(); }

    [MenuItem(Menu + "Step/3 Mesh LOD Import On")]
    public static void MenuMeshLodOn() => SetMeshLodImport(true);

    [MenuItem(Menu + "Step/4 Batching Static Off (Mesh LOD set)")]
    public static void MenuBatchOff() { SetBatchingStatic(Category.MeshLodSet, false); SaveScene(); }

    [MenuItem(Menu + "Revert/Batching Static On (Mesh LOD set)")]
    public static void MenuBatchOn() { SetBatchingStatic(Category.MeshLodSet, true); SaveScene(); }

    [MenuItem(Menu + "Revert/Mesh LOD Import Off")]
    public static void MenuMeshLodOff() => SetMeshLodImport(false);

    [MenuItem(Menu + "Dump Classification")]
    public static void DumpClassification()
    {
        var units = GetUnits();
        foreach (var g in units.GroupBy(u => Classify(UnitKey(u))).OrderBy(g => g.Key))
        {
            var names = g.GroupBy(UnitKey).Select(x => $"{x.Key} x{x.Count()}");
            Log($"[분류] {g.Key} ({g.Count()}): {string.Join(", ", names)}");
        }
    }

    // ───────── 파이프라인 ─────────

    private static void RunStep(int step)
    {
        try
        {
            switch (step)
            {
                case 0:
                    DumpClassification();
                    StartBench("00_baseline");
                    break;
                case 1:
                    ApplyOcclusionFlags();
                    BakeOcclusion();
                    SaveScene();
                    StartBench("01_occlusion");
                    break;
                case 2:
                    AddLodGroups();
                    BakeOcclusion();
                    SaveScene();
                    StartBench("02_lodgroup");
                    break;
                case 3:
                    SetMeshLodImport(true);
                    StartBench("03_meshlod_batchOn");
                    break;
                case 4:
                    SetBatchingStatic(Category.MeshLodSet, false);
                    SaveScene();
                    StartBench("04_meshlod_batchOff");
                    break;
                default:
                    SessionState.SetInt(StepKey, -1);
                    Log("===== 전체 파이프라인 끝 =====");
                    break;
            }
        }
        catch (Exception e)
        {
            SessionState.SetInt(StepKey, -1);
            Log($"단계 {step}에서 예외로 중단: {e}");
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        if (SessionState.GetBool(RunInBgSetKey, false))
        {
            SessionState.SetBool(RunInBgSetKey, false);
            PlayerSettings.runInBackground = SessionState.GetBool(RunInBgOrigKey, false);
        }

        if (SessionState.GetBool(RestoreMaxKey, false))
        {
            SessionState.SetBool(RestoreMaxKey, false);
            SetGameViewMaximized(false);
        }

        bool done = SessionState.GetBool(CullingBenchmarkRunner.DoneKey, false);
        SessionState.SetBool(CullingBenchmarkRunner.DoneKey, false);
        int step = SessionState.GetInt(StepKey, -1);
        if (step < 0)
        {
            return;
        }
        if (!done)
        {
            Log($"단계 {step} 벤치마크가 끝나지 않고 플레이 모드가 종료됨 - 'Continue Pipeline'으로 이 단계부터 다시 실행할 수 있음");
            return;
        }

        int next = step + 1;
        SessionState.SetInt(StepKey, next);
        EditorApplication.delayCall += () => RunStep(next);
    }

    private static void StartBench(string label)
    {
        if (!CheckScene())
        {
            return;
        }
        Log($"[{label}] 벤치마크 시작 요청");
        SessionState.SetBool(CullingBenchmarkRunner.DoneKey, false);
        SessionState.SetString(CullingBenchmarkRunner.PendingKey, label);
        SessionState.SetBool(RestoreMaxKey, SetGameViewMaximized(true));
        // 에디터가 포커스를 잃어도 측정이 멈추지 않게 잠시 켜 두고, 끝나면 원래 값으로 되돌린다.
        SessionState.SetBool(RunInBgOrigKey, PlayerSettings.runInBackground);
        SessionState.SetBool(RunInBgSetKey, true);
        PlayerSettings.runInBackground = true;
        EditorApplication.EnterPlaymode();
    }

    // 씬 뷰가 함께 그려지면 렌더 통계에 섞이므로 측정 중에는 Game 뷰를 최대화한다.
    private static bool SetGameViewMaximized(bool maximized)
    {
        try
        {
            Type gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var windows = Resources.FindObjectsOfTypeAll(gameViewType);
            if (windows.Length == 0)
            {
                return false;
            }
            var gv = (EditorWindow)windows[0];
            if (gv.maximized == maximized)
            {
                return false;
            }
            gv.maximized = maximized;
            return true;
        }
        catch (Exception e)
        {
            Log("Game 뷰 최대화 실패: " + e.Message);
            return false;
        }
    }

    private static bool CheckScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "GameScene")
        {
            Log($"활성 씬이 GameScene이 아님 ({scene.name})");
            return false;
        }
        if (GameObject.Find(CullingBenchmarkRunner.MapStaticPath) == null)
        {
            Log("Map/Map_v1/Static을 찾지 못함");
            return false;
        }
        return true;
    }

    private static void SaveScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Log("씬 저장");
    }

    // ───────── 대상 수집, 분류 ─────────

    // Static 아래의 "단위" 오브젝트: 가장 바깥 프리팹 인스턴스, 또는 렌더러를 가진 일반 오브젝트
    public static List<GameObject> GetUnits()
    {
        var root = GameObject.Find(CullingBenchmarkRunner.MapStaticPath);
        var list = new List<GameObject>();
        if (root != null)
        {
            foreach (Transform child in root.transform)
            {
                Collect(child, list);
            }
        }
        return list;
    }

    private static void Collect(Transform t, List<GameObject> list)
    {
        if (PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) || t.GetComponent<Renderer>() != null)
        {
            list.Add(t.gameObject);
            return;
        }
        foreach (Transform child in t)
        {
            Collect(child, list);
        }
    }

    public static string UnitKey(GameObject go)
    {
        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
        return string.IsNullOrEmpty(path) ? go.name : Path.GetFileNameWithoutExtension(path);
    }

    public static Category Classify(string key)
    {
        if (key.StartsWith("Road_set")) return Category.Ground;
        if (key.StartsWith("Cargo_container_v1_LD2")) return Category.Container;
        if (key.StartsWith("Palet_v1") || key.StartsWith("Hangar_") || key.StartsWith("Oil_tank_")
            || key.StartsWith("Pipes_set_v1") || key.StartsWith("Industrial_pipe")) return Category.MeshLodSet;
        if (key.StartsWith("Wooden_box") || key.StartsWith("Barrel_") || key.StartsWith("Dumpsters")
            || key.StartsWith("Road_block") || key.StartsWith("Conditioner") || key.StartsWith("Electric_box")
            || key.StartsWith("Generator") || key.StartsWith("Bags_on_pallet")) return Category.SmallProp;
        return Category.Large;
    }

    private static Bounds UnitBounds(GameObject unit)
    {
        var renderers = unit.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(unit.transform.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    // ───────── 1. 오클루전 ─────────

    // 바닥과 작은 소품은 가리개(Occluder)로 쓰지 않고 가려지는 쪽(Occludee)만 한다.
    public static void ApplyOcclusionFlags()
    {
        int occluders = 0, occludeeOnly = 0;
        foreach (var unit in GetUnits())
        {
            string key = UnitKey(unit);
            Category cat = Classify(key);
            Vector3 size = UnitBounds(unit).size;
            bool thinOrSmall = Mathf.Max(size.x, size.z) < 4f || size.y < 1.5f;
            bool occluder = cat != Category.Ground && cat != Category.SmallProp && !thinOrSmall
                            && !key.StartsWith("Palet_v1") && !key.StartsWith("Pipes_set_v1") && !key.StartsWith("Industrial_pipe");

            foreach (var t in unit.GetComponentsInChildren<Transform>(true))
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                flags |= StaticEditorFlags.OccludeeStatic;
                if (occluder) flags |= StaticEditorFlags.OccluderStatic;
                else flags &= ~StaticEditorFlags.OccluderStatic;
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
            }
            if (occluder) occluders++; else occludeeOnly++;
        }
        Log($"오클루전 플래그: Occluder+Occludee {occluders}개, Occludee만 {occludeeOnly}개");
    }

    public static void BakeOcclusion()
    {
        StaticOcclusionCulling.smallestOccluder = 2f;
        StaticOcclusionCulling.smallestHole = 0.25f;
        StaticOcclusionCulling.backfaceThreshold = 100f;
        var sw = Stopwatch.StartNew();
        bool ok = StaticOcclusionCulling.Compute();
        sw.Stop();
        Log($"오클루전 베이크 {(ok ? "성공" : "실패")}: {sw.Elapsed.TotalSeconds:F1}초, 데이터 {StaticOcclusionCulling.umbraDataSize / 1024f:F1} KB (smallestOccluder 2, smallestHole 0.25)");
    }

    // ───────── 2. LOD Group ─────────

    public static void AddLodGroups()
    {
        int containers = 0, props = 0, skipped = 0;
        foreach (var unit in GetUnits())
        {
            if (unit.GetComponent<LODGroup>() != null)
            {
                skipped++;
                continue;
            }
            Category cat = Classify(UnitKey(unit));
            if (cat == Category.Container)
            {
                if (AddContainerLod(unit)) containers++; else skipped++;
            }
            else if (cat == Category.SmallProp)
            {
                AddCullOnlyLod(unit);
                props++;
            }
        }
        Log($"LOD Group 추가: 컨테이너(LD2→LD1→Culled) {containers}개, 소품(LOD0→Culled) {props}개, 건너뜀 {skipped}개");
    }

    // 팩에 들어 있는 저사양 메시(LD1)를 LOD1로 쓴다.
    private static bool AddContainerLod(GameObject unit)
    {
        var mf = unit.GetComponentInChildren<MeshFilter>();
        var mr = mf != null ? mf.GetComponent<MeshRenderer>() : null;
        if (mf == null || mr == null || mf.sharedMesh == null) return false;

        string lowName = mf.sharedMesh.name.Replace("_LD2", "_LD1");
        string fbxPath = AssetDatabase.GetAssetPath(mf.sharedMesh);
        Mesh low = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Mesh>().FirstOrDefault(m => m.name == lowName);
        if (low == null)
        {
            Log($"{unit.name}: 저사양 메시 {lowName}을 찾지 못함");
            return false;
        }

        var child = new GameObject("LOD1_" + lowName);
        Undo.RegisterCreatedObjectUndo(child, "Add LOD1");
        child.transform.SetParent(mf.transform, false);
        child.layer = mf.gameObject.layer;
        child.AddComponent<MeshFilter>().sharedMesh = low;
        var lowRenderer = child.AddComponent<MeshRenderer>();
        lowRenderer.sharedMaterials = mr.sharedMaterials;
        lowRenderer.shadowCastingMode = mr.shadowCastingMode;
        lowRenderer.receiveShadows = mr.receiveShadows;
        GameObjectUtility.SetStaticEditorFlags(child, GameObjectUtility.GetStaticEditorFlags(mf.gameObject));

        var group = Undo.AddComponent<LODGroup>(unit);
        group.fadeMode = LODFadeMode.None;
        group.SetLODs(new[]
        {
            new LOD(0.12f, new Renderer[] { mr }),
            new LOD(0.02f, new Renderer[] { lowRenderer }),
        });
        group.RecalculateBounds();
        return true;
    }

    // 작은 소품은 화면에서 충분히 작아지면 그리지 않는다(크기에 따라 약 60~120m).
    private static void AddCullOnlyLod(GameObject unit)
    {
        var renderers = unit.GetComponentsInChildren<Renderer>(true);
        var group = Undo.AddComponent<LODGroup>(unit);
        group.fadeMode = LODFadeMode.None;
        group.SetLODs(new[] { new LOD(0.02f, renderers) });
        group.RecalculateBounds();

        float size = group.size;
        float cullDistance = size < 1.5f ? 60f : size < 3f ? 90f : 120f;
        float tan = Mathf.Tan(30f * Mathf.Deg2Rad);
        float height = Mathf.Clamp(size / (2f * cullDistance * tan), 0.004f, 0.2f);
        group.SetLODs(new[] { new LOD(height, renderers) });
    }

    // ───────── 3. Mesh LOD ─────────

    private static IEnumerable<string> MeshLodModelPaths()
    {
        return GetUnits()
            .Where(u => Classify(UnitKey(u)) == Category.MeshLodSet)
            .SelectMany(u => u.GetComponentsInChildren<MeshFilter>(true))
            .Where(mf => mf.sharedMesh != null)
            .Select(mf => AssetDatabase.GetAssetPath(mf.sharedMesh))
            .Where(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .Distinct();
    }

    public static void SetMeshLodImport(bool enable)
    {
        PropertyInfo prop = typeof(ModelImporter).GetProperty("generateMeshLods");
        if (prop == null)
        {
            Log("ModelImporter.generateMeshLods가 없는 Unity 버전 - Mesh LOD 건너뜀");
            return;
        }
        PropertyInfo lodCountProp = typeof(Mesh).GetProperty("lodCount");

        var paths = MeshLodModelPaths().ToList();
        var sw = Stopwatch.StartNew();
        foreach (var path in paths)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;
            if ((bool)prop.GetValue(importer) != enable)
            {
                prop.SetValue(importer, enable);
                importer.SaveAndReimport();
            }
            if (lodCountProp != null)
            {
                var info = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>()
                    .Select(m => $"{m.name}(LOD {lodCountProp.GetValue(m)}, tris {Enumerable.Range(0, m.subMeshCount).Sum(i => (long)m.GetIndexCount(i)) / 3})");
                Log($"Mesh LOD {(enable ? "켬" : "끔")} {Path.GetFileName(path)}: {string.Join(", ", info)}");
            }
        }
        sw.Stop();
        Log($"Mesh LOD 임포트 설정 {(enable ? "켬" : "끔")}: 모델 {paths.Count}개, {sw.Elapsed.TotalSeconds:F1}초");
    }

    public static void SetBatchingStatic(Category category, bool on)
    {
        int count = 0;
        foreach (var unit in GetUnits().Where(u => Classify(UnitKey(u)) == category))
        {
            foreach (var t in unit.GetComponentsInChildren<Transform>(true))
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                if (on) flags |= StaticEditorFlags.BatchingStatic;
                else flags &= ~StaticEditorFlags.BatchingStatic;
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
            }
            count++;
        }
        Log($"{category} {count}개 Batching Static {(on ? "켬" : "끔")}");
    }
}
