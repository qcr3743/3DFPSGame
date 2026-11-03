using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// LoginScene의 UI 계층을 코드로 만든다. (.unity 파일을 직접 편집하지 않기 위해 에디터 메뉴로 생성)
// 사용법: LoginScene을 연 상태에서 Tools > Login > Build Login UI 실행 → Ctrl+S로 씬 저장
//
// 파일 구성 (partial class)
//  - LoginSceneBuilder.cs         : 상수, 메뉴, 전체 흐름, 카메라/볼륨/EventSystem
//  - LoginSceneBuilder.Sprites.cs : 코드로 만드는 sprite (그라데이션, 비네트, 버튼 모양 등)
//  - LoginSceneBuilder.Layout.cs  : Canvas_Login 계층과 UI 위젯 생성 헬퍼
public static partial class LoginSceneBuilder
{
    private const string MenuPath = "Tools/Login/Build Login UI";
    private const string TargetSceneName = "LoginScene";
    private const string CanvasName = "Canvas_Login";
    private const string VolumeName = "Global Volume";
    private const string UndoName = "Build Login UI";

    // ───── 폰트 경로 ─────
    // 지금은 기존 LevelUp 폰트 하나로 통일한다. 나중에 폰트를 나누려면 여기 경로만 바꾸면 된다.
    // 에셋이 없으면 TMP 기본 폰트로 대체한다.
    private const string FontTitlePath = "Assets/08.Fonts/LevelUpFont/LevelUp-Regular SDF.asset";      // 타이틀 (원래 Chakra Petch Bold)
    private const string FontMonoPath = "Assets/08.Fonts/LevelUpFont/LevelUp-Regular SDF.asset";       // HUD/장식 (원래 JetBrains Mono)
    private const string FontKrRegularPath = "Assets/08.Fonts/LevelUpFont/LevelUp-Regular SDF.asset";  // 한글 본문 Regular
    private const string FontKrMediumPath = "Assets/08.Fonts/LevelUpFont/LevelUp-Regular SDF.asset";   // 한글 본문 Medium
    private const string FontKrSemiBoldPath = "Assets/08.Fonts/LevelUpFont/LevelUp-Regular SDF.asset"; // 한글 본문 SemiBold

    // ───── 에셋 경로 ─────
    private const string ImageFolder = "Assets/04.Images/Login";
    private const string ProfileFolder = "Assets/Settings/Login";
    private const string ProfilePath = ProfileFolder + "/LoginVolumeProfile.asset";

    // ───── 디자인 토큰 ─────
    private static readonly Color Ground = Hex("#070908");
    private static readonly Color TextPrimary = Hex("#F2F3EF");
    private static readonly Color TextSecondary = Hex("#B4BCB5");
    private static readonly Color TextMuted = Hex("#9AA39C");
    private static readonly Color TextDim = Hex("#7C857E");
    private static readonly Color Accent = Hex("#9EE62E");
    private static readonly Color AccentText = Hex("#0B0D0C");
    private static readonly Color Error = Hex("#FF6A4D");          // 오류 테두리 (내일 연결)
    private static readonly Color ErrorText = Hex("#FF8A70");
    private static readonly Color Line = new Color(1f, 1f, 1f, 0.14f);
    // 시안 값은 0.04지만 프로젝트가 Linear 색 공간이라 그대로 쓰면 밝은 회색이 된다.
    // #070908 위에서 시안(sRGB 블렌딩)과 같은 밝기가 나오도록 환산한 값.
    private static readonly Color FieldBg = new Color(1f, 1f, 1f, 0.004f);
    private static readonly Color FieldBorder = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color FieldBorderHover = new Color(1f, 1f, 1f, 0.32f);
    private static readonly Color BgImageTint = Hex("#DADADA");    // 배경 이미지 밝기 조절 (UI 녹색이 탁해지지 않게 Volume 대신 여기서)
    private const float ScanlineOpacity = 0.7f;                    // 스캔라인 텍스처(alpha 0.03)에 곱하는 비율

    // 빌드 중에만 쓰는 공유 값 (Build 시작 시 채운다)
    private static TMP_FontAsset _fontTitle;
    private static TMP_FontAsset _fontMono;
    private static TMP_FontAsset _fontKrRegular;
    private static TMP_FontAsset _fontKrMedium;
    private static TMP_FontAsset _fontKrSemiBold;
    private static int _uiLayer;
    private static AudioSource _uiAudio;

    [MenuItem(MenuPath)]
    private static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Build Login UI", "플레이 모드에서는 실행할 수 없습니다.", "확인");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != TargetSceneName)
        {
            EditorUtility.DisplayDialog("Build Login UI",
                $"현재 활성 씬이 '{scene.name}'입니다.\n{TargetSceneName}을 연 뒤 다시 실행하세요.", "확인");
            return;
        }

        GameObject existing = FindRoot(scene, CanvasName);
        if (existing != null &&
            !EditorUtility.DisplayDialog("Build Login UI",
                $"{CanvasName}이(가) 이미 있습니다. 지우고 다시 만들까요?", "다시 만들기", "취소"))
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);

        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        _uiLayer = LayerMask.NameToLayer("UI");
        if (_uiLayer < 0)
        {
            _uiLayer = 0;
        }

        LoadFonts();
        CreateSprites();

        Camera camera = SetupCamera();
        Volume volume = SetupVolume(scene);
        SetupEventSystem();
        BuildCanvas(camera, volume);

        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log("LoginScene UI 생성 완료 — Ctrl+S로 씬 저장");
    }

    // ───── 폰트 ─────

    private static void LoadFonts()
    {
        var missing = new List<string>();
        _fontTitle = LoadFont(FontTitlePath, missing);
        _fontMono = LoadFont(FontMonoPath, missing);
        _fontKrRegular = LoadFont(FontKrRegularPath, missing);
        _fontKrMedium = LoadFont(FontKrMediumPath, missing);
        _fontKrSemiBold = LoadFont(FontKrSemiBoldPath, missing);

        if (missing.Count > 0)
        {
            Debug.LogWarning("[LoginSceneBuilder] 폰트 에셋이 없어 TMP 기본 폰트로 대체했습니다:\n" +
                             string.Join("\n", new HashSet<string>(missing)));
        }
    }

    private static TMP_FontAsset LoadFont(string path, List<string> missing)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font != null)
        {
            return font;
        }

        missing.Add(path);
        return TMP_Settings.defaultFontAsset;
    }

    // ───── 카메라 ─────

    // 기존 Main Camera를 재사용해서 배경색과 Post Processing만 설정한다.
    private static Camera SetupCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            Undo.RegisterCreatedObjectUndo(cameraGo, UndoName);
            camera = cameraGo.GetComponent<Camera>();
            Debug.LogWarning("[LoginSceneBuilder] Main Camera가 없어 새로 만들었습니다.");
        }

        Undo.RecordObject(camera, UndoName);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Ground;

        // Screen Space - Camera 캔버스에 Bloom/Grain이 먹으려면 카메라의 Post Processing이 켜져 있어야 한다.
        UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
        Undo.RecordObject(cameraData, UndoName);
        cameraData.renderPostProcessing = true;
        EditorUtility.SetDirty(cameraData);

        return camera;
    }

    // ───── 포스트 프로세싱 ─────

    private static Volume SetupVolume(Scene scene)
    {
        VolumeProfile profile = CreateOrLoadProfile();

        GameObject volumeGo = FindRoot(scene, VolumeName);
        if (volumeGo == null)
        {
            volumeGo = new GameObject(VolumeName);
            Undo.RegisterCreatedObjectUndo(volumeGo, UndoName);
        }

        Volume volume = volumeGo.GetComponent<Volume>();
        if (volume == null)
        {
            volume = Undo.AddComponent<Volume>(volumeGo);
        }

        Undo.RecordObject(volume, UndoName);
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.sharedProfile = profile;

        return volume;
    }

    private static VolumeProfile CreateOrLoadProfile()
    {
        EnsureFolder(ProfileFolder);

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        ColorAdjustments colorAdjustments = GetOrAddEffect<ColorAdjustments>(profile);
        // 채도/노출은 건드리지 않는다 (UI 녹색이 탁해지지 않게). 배경 밝기는 BG_Image 색으로 조절.
        colorAdjustments.saturation.Override(0f);
        colorAdjustments.contrast.Override(12f);
        colorAdjustments.postExposure.Override(0f);

        Bloom bloom = GetOrAddEffect<Bloom>(profile);
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.6f);

        Vignette vignette = GetOrAddEffect<Vignette>(profile);
        vignette.intensity.Override(0.28f);
        vignette.smoothness.Override(0.4f);

        FilmGrain filmGrain = GetOrAddEffect<FilmGrain>(profile);
        filmGrain.type.Override(FilmGrainLookup.Medium1);
        filmGrain.intensity.Override(0.25f);

        // 평소 0. 신호 간섭 때 UI_PanelSwitcher가 잠깐 올린다 (Override가 켜져 있어야 스크립트 값이 적용됨).
        ChromaticAberration chromatic = GetOrAddEffect<ChromaticAberration>(profile);
        chromatic.intensity.Override(0f);

        foreach (VolumeComponent component in profile.components)
        {
            EditorUtility.SetDirty(component);
        }
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        return profile;
    }

    // 프로필 에셋에 효과를 추가할 때는 서브 에셋으로 함께 저장해야 다시 열어도 남아 있다.
    private static T GetOrAddEffect<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T component))
        {
            return component;
        }

        component = profile.Add<T>(false);
        component.name = typeof(T).Name;
        component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    // ───── EventSystem ─────

    private static void SetupEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        // 프로젝트는 레거시 Input API를 쓰므로(Active Input Handling: Both) StandaloneInputModule을 사용한다.
        var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystemGo, UndoName);
    }

    // ───── 공용 유틸 ─────

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }
        return null;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];  // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    // private [SerializeField] 필드는 코드에서 바로 넣을 수 없으므로 SerializedObject로 연결한다.
    private static void SetField(Object target, string fieldName, Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogError($"[LoginSceneBuilder] {target.GetType().Name}.{fieldName} 필드를 찾을 수 없습니다.");
            return;
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFieldArray(Object target, string fieldName, Object[] values)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null || !property.isArray)
        {
            Debug.LogError($"[LoginSceneBuilder] {target.GetType().Name}.{fieldName} 배열 필드를 찾을 수 없습니다.");
            return;
        }

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColorField(Object target, string fieldName, Color value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null || property.propertyType != SerializedPropertyType.Color)
        {
            Debug.LogError($"[LoginSceneBuilder] {target.GetType().Name}.{fieldName} 색 필드를 찾을 수 없습니다.");
            return;
        }

        property.colorValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
