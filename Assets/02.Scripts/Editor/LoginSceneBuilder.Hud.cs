using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 배경(BG), HUD_Top, TargetBracket, HUD_Bottom, FX_Overlay 생성.
public static partial class LoginSceneBuilder
{
    private const float HudSideMargin = 120f;
    private const float HudFontSize = 13f;
    private const float HudCharacterSpacing = 12f;

    // ───── 배경 ─────
    private static void BuildBackground(Transform parent)
    {
        RectTransform bg = CreateRect("BG", parent);
        Stretch(bg);

        // 배경 이미지 (BG_Login.jpg). 파일이 없으면 Image를 꺼 둬서 회색 사각형이 보이지 않게 한다.
        Image bgImage = CreateImage("BG_Image", bg, _spriteBackground, BgImageTint, false);
        Stretch(bgImage.rectTransform);
        bgImage.enabled = _spriteBackground != null;
        bgImage.gameObject.AddComponent<UI_BackgroundDrift>();

        Image leftShade = CreateImage("BG_LeftShade", bg, _spriteLeftShade, Color.white, false);
        Stretch(leftShade.rectTransform);

        Image vignette = CreateImage("BG_Vignette", bg, _spriteVignette, Color.white, false);
        Stretch(vignette.rectTransform);

        Image scanline = CreateImage("BG_Scanline", bg, _spriteScanline, Color.white, false);
        scanline.type = Image.Type.Tiled;
        Stretch(scanline.rectTransform);
    }

    // ───── HUD_Top (왼쪽 120 / 오른쪽 120 / 위 44, 높이 24) ─────
    // 인트로에서 그어질 가로선 RectTransform을 돌려준다.
    private static RectTransform BuildHudTop(Transform parent)
    {
        RectTransform hud = CreateRect("HUD_Top", parent);
        hud.anchorMin = new Vector2(0f, 1f);
        hud.anchorMax = new Vector2(1f, 1f);
        hud.pivot = new Vector2(0.5f, 1f);
        hud.sizeDelta = new Vector2(-HudSideMargin * 2f, 24f);
        hud.anchoredPosition = new Vector2(0f, -44f);

        // 왼쪽: ● LIVE  SECTOR 07 // QUARANTINE ZONE
        RectTransform left = CreateRect("HUD_Top_Left", hud);
        Stretch(left);
        AddHorizontal(left.gameObject, TextAnchor.MiddleLeft, 12f);

        Image liveDot = CreateImage("Dot_Live", left, _spriteDot, Accent, false);
        SetLayoutSize(liveDot.gameObject, 8f, 8f);
        liveDot.gameObject.AddComponent<UI_Blink>();

        CreateHudText("Text_Live", left, "LIVE", Accent);
        CreateHudText("Text_Sector", left, "SECTOR 07 // QUARANTINE ZONE", TextMuted);

        // 오른쪽: 좌표 · UTC 시각
        RectTransform right = CreateRect("HUD_Top_Right", hud);
        Stretch(right);
        AddHorizontal(right.gameObject, TextAnchor.MiddleRight, 32f);

        CreateHudText("Text_Coordinates", right, "41.3111° N  69.2797° E", TextMuted);
        TextMeshProUGUI clock = CreateHudText("Text_Clock", right, "UTC 05:53:07", TextMuted);
        clock.gameObject.AddComponent<UI_ClockText>();

        // y = 84 가로선 (HUD_Top 위쪽 44 기준으로 40 아래). 인트로에서 왼쪽부터 그어지도록 pivot x = 0.
        Image line = CreateImage("HUD_TopLine", hud, null, Line, false);
        RectTransform lineRect = line.rectTransform;
        lineRect.anchorMin = new Vector2(0f, 1f);
        lineRect.anchorMax = new Vector2(1f, 1f);
        lineRect.pivot = new Vector2(0f, 1f);
        lineRect.sizeDelta = new Vector2(0f, 1f);
        lineRect.anchoredPosition = new Vector2(0f, -40f);

        return lineRect;
    }

    private static TextMeshProUGUI CreateHudText(string name, Transform parent, string text, Color color)
    {
        return CreateText(name, parent, text, _fontMono, HudFontSize, color, TextAlignmentOptions.Left,
            FontStyles.Normal, HudCharacterSpacing);
    }

    // ───── TargetBracket (x 1290, y 520, 360x120) ─────
    // 배경 이미지의 좀비 무리 위치에 맞춘 조준 표시. 네 모서리 L자 + 위쪽 태그.
    private static void BuildTargetBracket(Transform parent)
    {
        RectTransform bracket = CreateRect("TargetBracket", parent);
        SetTopLeft(bracket, 1290f, 520f, 360f, 120f);

        CreateBracketCorner(bracket, "Corner_TL", new Vector2(0f, 1f));
        CreateBracketCorner(bracket, "Corner_TR", new Vector2(1f, 1f));
        CreateBracketCorner(bracket, "Corner_BL", new Vector2(0f, 0f));
        CreateBracketCorner(bracket, "Corner_BR", new Vector2(1f, 0f));

        // 태그: 위쪽 -30 (태그 윗변이 브래킷 위 30px, 높이 22)
        RectTransform tagRow = CreateRect("Tag", bracket);
        tagRow.anchorMin = new Vector2(0f, 1f);
        tagRow.anchorMax = new Vector2(0f, 1f);
        tagRow.pivot = new Vector2(0f, 0f);
        tagRow.sizeDelta = new Vector2(360f, 22f);
        tagRow.anchoredPosition = new Vector2(0f, 8f);
        AddHorizontal(tagRow.gameObject, TextAnchor.MiddleLeft, 10f, true);

        Image hostile = CreateImage("Tag_Hostile", tagRow, null, Accent, false);
        HorizontalLayoutGroup hostileLayout = AddHorizontal(hostile.gameObject, TextAnchor.MiddleCenter, 0f, true);
        hostileLayout.padding = new RectOffset(8, 8, 0, 0);
        CreateText("Text", hostile.transform, "HOSTILE ×3", _fontMono, 12f, AccentText,
            TextAlignmentOptions.Center, FontStyles.Normal, 2f);

        CreateText("Text_Distance", tagRow, "DIST 240m", _fontMono, 12f, Accent,
            TextAlignmentOptions.Left, FontStyles.Normal, 2f);
    }

    // corner(0/1, 0/1) 위치에 22px L자 선 (두께 2)
    private static void CreateBracketCorner(Transform parent, string name, Vector2 corner)
    {
        const float length = 22f;
        const float thickness = 2f;

        RectTransform root = CreateRect(name, parent);
        root.anchorMin = corner;
        root.anchorMax = corner;
        root.pivot = corner;
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(length, length);

        CreateCornerBar(root, "Horizontal", corner, new Vector2(length, thickness));
        CreateCornerBar(root, "Vertical", corner, new Vector2(thickness, length));
    }

    private static void CreateCornerBar(Transform parent, string name, Vector2 corner, Vector2 size)
    {
        Image bar = CreateImage(name, parent, null, Accent, false);
        RectTransform rect = bar.rectTransform;
        rect.anchorMin = corner;
        rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    // ───── HUD_Bottom (왼쪽 120 / 오른쪽 120 / 아래 44) ─────
    private static void BuildHudBottom(Transform parent)
    {
        RectTransform hud = CreateRect("HUD_Bottom", parent);
        hud.anchorMin = new Vector2(0f, 0f);
        hud.anchorMax = new Vector2(1f, 0f);
        hud.pivot = new Vector2(0.5f, 0f);
        hud.sizeDelta = new Vector2(-HudSideMargin * 2f, 44f);
        hud.anchoredPosition = new Vector2(0f, 44f);

        TextMeshProUGUI version = CreateText("Text_Version", hud, "v0.1 · BUILD 2026.11", _fontMono, 12f, TextDim,
            TextAlignmentOptions.Left, FontStyles.Normal, HudCharacterSpacing);
        Stretch(version.rectTransform);

        // 오른쪽: SERVER ● ONLINE  [게임 종료]
        RectTransform right = CreateRect("HUD_Bottom_Right", hud);
        Stretch(right);
        AddHorizontal(right.gameObject, TextAnchor.MiddleRight, 10f);

        CreateText("Text_Server", right, "SERVER", _fontMono, 12f, TextDim, TextAlignmentOptions.Left,
            FontStyles.Normal, HudCharacterSpacing);

        Image serverDot = CreateImage("Dot_Server", right, _spriteDot, Accent, false);
        SetLayoutSize(serverDot.gameObject, 6f, 6f);
        serverDot.gameObject.AddComponent<UI_Blink>();

        CreateText("Text_Online", right, "ONLINE", _fontMono, 12f, TextDim, TextAlignmentOptions.Left,
            FontStyles.Normal, HudCharacterSpacing);

        RectTransform gap = CreateRect("Gap", right);
        SetLayoutSize(gap.gameObject, 14f, 1f);

        // 게임 종료 버튼 (OnClick은 내일 연결)
        Button quitButton = CreateOutlineButton(right, "Button_Quit", "게임 종료", 13f);
        SetLayoutSize(quitButton.gameObject, 120f, 44f);
    }

    // ───── FX_Overlay ─────
    // 인트로 검은 화면 / 패널 전환 신호 간섭용. 평소 alpha 0이고 클릭을 막지 않는다.
    private static CanvasGroup BuildFxOverlay(Transform parent, out TMP_Text connectingText)
    {
        RectTransform fx = CreateRect("FX_Overlay", parent);
        Stretch(fx);

        var group = fx.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;   // 인트로 중에만 UI_IntroSequence가 true로 바꿔 클릭을 막는다

        Image black = CreateImage("FX_Black", fx, null, Ground, true);
        Stretch(black.rectTransform);

        TextMeshProUGUI connecting = CreateText("FX_Connecting", fx, "CONNECTING...", _fontMono, 16f, TextMuted,
            TextAlignmentOptions.Center, FontStyles.Normal, HudCharacterSpacing);
        Stretch(connecting.rectTransform);
        connecting.gameObject.SetActive(false);

        connectingText = connecting;
        return group;
    }
}
