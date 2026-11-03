using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 패널에서 반복해서 쓰는 위젯: 헤더, 구분선, 라벨, 입력칸, 토글, 버튼(주/외곽선/링크), 푸터.
public static partial class LoginSceneBuilder
{
    private const float InputHeight = 56f;
    private const float InputPadding = 18f;
    private const float InputFontSize = 17f;
    private const float PrimaryButtonHeight = 60f;

    // 왼쪽 큰 제목 + 오른쪽 단계 표시 (예: 로그인 / 01 / SIGN IN)
    private static void CreateHeader(Transform parent, string title, string step)
    {
        RectTransform row = CreateRect("Header", parent);
        SetLayoutSize(row.gameObject, -1f, 32f);

        TextMeshProUGUI titleText = CreateText("Text_Title", row, title, _fontKrSemiBold, 24f, TextPrimary,
            TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        Stretch(titleText.rectTransform);

        TextMeshProUGUI stepText = CreateText("Text_Step", row, step, _fontMono, 12f, TextDim,
            TextAlignmentOptions.BottomRight, FontStyles.Normal, 2f);
        Stretch(stepText.rectTransform);
    }

    private static void CreateDivider(Transform parent)
    {
        Image divider = CreateImage("Divider", parent, null, Line, false);
        SetLayoutSize(divider.gameObject, -1f, 1f);
    }

    private static void CreateLabel(Transform parent, string name, string text)
    {
        CreateText(name, parent, text, _fontKrMedium, 14f, TextSecondary, TextAlignmentOptions.TopLeft);
    }

    // ───── 입력칸 ─────
    // 구조: Input(바탕 Image + TMP_InputField + UI_InputFieldFeel)
    //       ├─ Border (1px 테두리, Sliced)
    //       └─ Text Area (RectMask2D, 좌우 padding 18)
    //          ├─ Placeholder
    //          └─ Text
    private static TMP_InputField CreateInputField(Transform parent, string name, string placeholder, bool isPassword)
    {
        RectTransform root = CreateRect(name, parent);
        SetLayoutSize(root.gameObject, -1f, InputHeight);

        var background = root.gameObject.AddComponent<Image>();
        background.color = FieldBg;
        background.raycastTarget = true;   // 클릭으로 포커스를 받는 영역

        Image border = CreateSlicedImage("Border", root, _spriteFrame, FieldBorder, false);
        Stretch(border.rectTransform);

        RectTransform textArea = CreateRect("Text Area", root);
        Stretch(textArea, InputPadding, InputPadding, 0f, 0f);
        textArea.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholderText = CreateText("Placeholder", textArea, placeholder, _fontKrRegular,
            InputFontSize, TextDim, TextAlignmentOptions.Left);
        Stretch(placeholderText.rectTransform);

        TextMeshProUGUI inputText = CreateText("Text", textArea, string.Empty, _fontKrRegular,
            InputFontSize, TextPrimary, TextAlignmentOptions.Left);
        Stretch(inputText.rectTransform);

        var input = root.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = background;
        input.transition = Selectable.Transition.None;   // 색 변화는 UI_InputFieldFeel이 담당
        input.textViewport = textArea;
        input.textComponent = inputText;
        input.placeholder = placeholderText;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = isPassword ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        input.richText = false;
        input.customCaretColor = true;
        input.caretColor = Accent;
        input.caretWidth = 2;
        input.selectionColor = WithAlpha(Accent, 0.35f);

        var feel = root.gameObject.AddComponent<UI_InputFieldFeel>();
        SetField(feel, "_inputField", input);
        SetField(feel, "_border", border);
        SetField(feel, "_background", background);
        SetField(feel, "_audioSource", _uiAudio);

        return input;
    }

    // ───── 토글 (아이디 저장) ─────
    private static Toggle CreateToggle(Transform parent, string name, string label)
    {
        RectTransform row = CreateRect(name + "_Row", parent);
        SetLayoutSize(row.gameObject, -1f, 18f);

        // 체크박스 + 라벨까지 클릭되도록 투명한 클릭 영역을 둔다.
        RectTransform root = CreateRect(name, row);
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(160f, 0f);

        var hitArea = root.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true;

        Image box = CreateImage("Box", root, null, FieldBg, false);
        RectTransform boxRect = box.rectTransform;
        boxRect.anchorMin = new Vector2(0f, 0.5f);
        boxRect.anchorMax = new Vector2(0f, 0.5f);
        boxRect.pivot = new Vector2(0f, 0.5f);
        boxRect.anchoredPosition = Vector2.zero;
        boxRect.sizeDelta = new Vector2(18f, 18f);

        Image boxBorder = CreateSlicedImage("Border", boxRect, _spriteFrame, FieldBorderHover, false);
        Stretch(boxBorder.rectTransform);

        Image checkmark = CreateImage("Checkmark", boxRect, null, Accent, false);
        Stretch(checkmark.rectTransform, 4f, 4f, 4f, 4f);

        TextMeshProUGUI labelText = CreateText("Label", root, label, _fontKrRegular, 15f, TextSecondary,
            TextAlignmentOptions.Left);
        Stretch(labelText.rectTransform, 28f, 0f, 0f, 0f);

        var toggle = root.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = hitArea;
        toggle.transition = Selectable.Transition.None;
        toggle.graphic = checkmark;
        toggle.SetIsOnWithoutNotify(false);

        return toggle;
    }

    // ───── 버튼 ─────
    // 구조: Button 루트(투명 클릭 영역, 레이아웃 대상) └─ Visual(실제 모습, UI_ButtonFeel이 움직임)
    private static Button CreateButtonRoot(Transform parent, string name, out RectTransform visual)
    {
        RectTransform root = CreateRect(name, parent);

        var hitArea = root.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true;

        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = hitArea;
        button.transition = Selectable.Transition.None;   // 밝기 변화는 UI_ButtonFeel이 담당

        visual = CreateRect("Visual", root);
        Stretch(visual);

        return button;
    }

    private static void AddButtonFeel(Button button, RectTransform visual, Graphic[] graphics)
    {
        var feel = button.gameObject.AddComponent<UI_ButtonFeel>();
        SetField(feel, "_moveTarget", visual);
        SetFieldArray(feel, "_graphics", graphics);
        SetField(feel, "_audioSource", _uiAudio);
    }

    // 주 버튼: Accent 바탕 + 깎인 모서리, 왼쪽 글자 / 오른쪽 → 화살표. OnClick은 비워 둔다.
    private static Button CreatePrimaryButton(Transform parent, string name, string label)
    {
        Button button = CreateButtonRoot(parent, name, out RectTransform visual);
        SetLayoutSize(button.gameObject, -1f, PrimaryButtonHeight);

        Image background = CreateSlicedImage("BG", visual, _spriteChamfer, Accent, true);
        Stretch(background.rectTransform);

        TextMeshProUGUI labelText = CreateText("Label", visual, label, _fontKrSemiBold, 19f, AccentText,
            TextAlignmentOptions.Left, FontStyles.Bold);
        Stretch(labelText.rectTransform, 24f, 24f, 0f, 0f);

        TextMeshProUGUI arrow = CreateText("Arrow", visual, "→", _fontMono, 20f, AccentText,
            TextAlignmentOptions.Right);
        Stretch(arrow.rectTransform, 24f, 24f, 0f, 0f);

        // 바탕만 밝기 변화 (글자는 어두운 색 유지)
        AddButtonFeel(button, visual, new Graphic[] { background });
        return button;
    }

    // 외곽선 버튼: 테두리만 있는 버튼 (중복 확인, 게임 종료). 크기는 호출하는 쪽에서 LayoutElement로 정한다.
    private static Button CreateOutlineButton(Transform parent, string name, string label, float fontSize)
    {
        Button button = CreateButtonRoot(parent, name, out RectTransform visual);

        Image border = CreateSlicedImage("Border", visual, _spriteFrame, FieldBorderHover, false);
        Stretch(border.rectTransform);

        TextMeshProUGUI labelText = CreateText("Label", visual, label, _fontKrMedium, fontSize, TextPrimary,
            TextAlignmentOptions.Center);
        Stretch(labelText.rectTransform);

        AddButtonFeel(button, visual, new Graphic[] { border, labelText });
        return button;
    }

    // 밑줄 링크 버튼 (회원가입 / 로그인). 폭은 글자 폭에 맞춘다.
    private static Button CreateLinkButton(Transform parent, string name, string label, float fontSize)
    {
        Button button = CreateButtonRoot(parent, name, out RectTransform visual);

        TextMeshProUGUI labelText = CreateText("Label", visual, label, _fontKrMedium, fontSize, Accent,
            TextAlignmentOptions.Left, FontStyles.Underline);
        Stretch(labelText.rectTransform);

        // 폰트에 따라 측정값이 작게 나올 수 있어 글자 수 기준 최소 폭을 함께 쓴다.
        float measured = labelText.GetPreferredValues(label).x;
        float width = Mathf.Ceil(Mathf.Max(measured, label.Length * fontSize)) + 2f;
        SetLayoutSize(button.gameObject, width, fontSize + 7f);

        AddButtonFeel(button, visual, new Graphic[] { labelText });
        return button;
    }

    // "계정이 없나요?  회원가입" 형태의 푸터. 링크 버튼을 돌려준다.
    private static Button CreateFooter(Transform parent, string message, string linkName, string linkLabel)
    {
        const float fontSize = 15f;

        RectTransform row = CreateRect("Footer", parent);
        SetLayoutSize(row.gameObject, -1f, 22f);
        AddHorizontal(row.gameObject, TextAnchor.MiddleLeft, 8f);

        CreateText("Text_Message", row, message, _fontKrRegular, fontSize, TextMuted, TextAlignmentOptions.Left);
        return CreateLinkButton(row, linkName, linkLabel, fontSize);
    }
}
