using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Canvas_Login 루트 생성과 연출 스크립트 연결, 그리고 RectTransform/Image/Text 기본 헬퍼.
// 패널/HUD 내용은 LoginPanel, SignupPanel, Hud 파일에, 입력칸·버튼 같은 위젯은 Widgets 파일에 있다.
public static partial class LoginSceneBuilder
{
    private const float PanelWidth = 480f;

    private static void BuildCanvas(Camera camera, Volume volume)
    {
        // ───── Canvas 루트 ─────
        RectTransform canvasRect = CreateRect(CanvasName, null);
        GameObject canvasGo = canvasRect.gameObject;

        // Overlay면 포스트 프로세싱이 UI에 안 먹으므로 Screen Space - Camera로 만든다.
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 10f;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // UI 효과음 공용 AudioSource (클립 슬롯은 각 Feel 스크립트에 있음)
        _uiAudio = canvasGo.AddComponent<AudioSource>();
        _uiAudio.playOnAwake = false;

        // ───── 자식 (아래에 만든 것이 위에 그려진다) ─────
        BuildBackground(canvasRect);
        RectTransform hudTopLine = BuildHudTop(canvasRect);
        BuildTargetBracket(canvasRect);
        CanvasGroup loginPanel = BuildLoginPanel(canvasRect, out Button linkSignup);
        CanvasGroup signupPanel = BuildSignupPanel(canvasRect, out Button linkLogin);
        BuildHudBottom(canvasRect);
        CanvasGroup fxOverlay = BuildFxOverlay(canvasRect, out TMP_Text connectingText);

        // ───── 연출 스크립트 연결 ─────
        var switcher = canvasGo.AddComponent<UI_PanelSwitcher>();
        SetField(switcher, "_loginPanel", loginPanel);
        SetField(switcher, "_signupPanel", signupPanel);
        SetField(switcher, "_fxOverlay", fxOverlay);
        SetField(switcher, "_volume", volume);

        var intro = canvasGo.AddComponent<UI_IntroSequence>();
        SetField(intro, "_fxOverlay", fxOverlay);
        SetField(intro, "_connectingText", connectingText);
        SetFieldArray(intro, "_hudLines", new Object[] { hudTopLine });
        SetField(intro, "_loginPanel", loginPanel);

        // 배경 클릭 사격 연출 (ShootCatcher는 BG 바로 위, ShootFX는 FX_Overlay 바로 앞에 끼워 넣는다)
        BuildShooting(canvasRect, intro);

        // 링크 버튼 OnClick만 패널 전환에 연결한다. (로그인/가입/중복 확인/게임 종료 버튼은 비워 둠)
        UnityEventTools.AddPersistentListener(linkSignup.onClick, switcher.ShowSignup);
        UnityEventTools.AddPersistentListener(linkLogin.onClick, switcher.ShowLogin);

        signupPanel.gameObject.SetActive(false);

        Undo.RegisterCreatedObjectUndo(canvasGo, UndoName);
    }

    // ───── RectTransform 헬퍼 ─────

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = _uiLayer;
        var rect = (RectTransform)go.transform;
        if (parent != null)
        {
            rect.SetParent(parent, false);
        }
        return rect;
    }

    // 시안 좌표(1920x1080, 좌상단 기준 px) 그대로 배치한다.
    private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    // 부모를 가득 채운다. 인자는 안쪽 여백.
    private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    // ───── 그래픽 헬퍼 ─────

    // 장식용은 raycast false, 클릭 영역만 true로 만든다.
    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycastTarget)
    {
        RectTransform rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static Image CreateSlicedImage(string name, Transform parent, Sprite sprite, Color color, bool fillCenter)
    {
        Image image = CreateImage(name, parent, sprite, color, false);
        image.type = Image.Type.Sliced;
        image.fillCenter = fillCenter;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, TMP_FontAsset font,
                                              float size, Color color, TextAlignmentOptions alignment,
                                              FontStyles style = FontStyles.Normal, float characterSpacing = 0f)
    {
        RectTransform rect = CreateRect(name, parent);
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            tmp.font = font;
        }
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        tmp.characterSpacing = characterSpacing;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        tmp.text = text;
        return tmp;
    }

    // ───── 레이아웃 헬퍼 ─────

    // 값이 0 미만이면 그 축은 건드리지 않는다.
    private static LayoutElement SetLayoutSize(GameObject go, float preferredWidth, float preferredHeight)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = go.AddComponent<LayoutElement>();
        }

        if (preferredWidth >= 0f)
        {
            element.minWidth = preferredWidth;
            element.preferredWidth = preferredWidth;
        }

        if (preferredHeight >= 0f)
        {
            element.minHeight = preferredHeight;
            element.preferredHeight = preferredHeight;
        }

        return element;
    }

    // 세로 Layout에서 "위 간격 N"을 고정 높이 빈 오브젝트로 만든다.
    private static void AddSpacer(Transform parent, float height)
    {
        RectTransform spacer = CreateRect("Spacer", parent);
        SetLayoutSize(spacer.gameObject, -1f, height);
    }

    private static HorizontalLayoutGroup AddHorizontal(GameObject go, TextAnchor alignment, float spacing,
                                                       bool expandHeight = false)
    {
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = alignment;
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = expandHeight;
        return layout;
    }

    // Panel_Login / Panel_Signup 공통: 좌상단 (x, y), 폭 480, 높이는 내용에 맞춰 자동.
    private static CanvasGroup CreatePanel(string name, Transform parent, float x, float y)
    {
        RectTransform rect = CreateRect(name, parent);
        SetTopLeft(rect, x, y, PanelWidth, 0f);

        var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 0f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rect.gameObject.AddComponent<CanvasGroup>();
    }
}
