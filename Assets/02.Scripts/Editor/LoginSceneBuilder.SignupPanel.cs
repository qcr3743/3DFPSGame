using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel_Signup (x 120, y 150, 폭 480). 비활성 상태로 시작한다 (BuildCanvas에서 끔).
public static partial class LoginSceneBuilder
{
    private const float CheckButtonWidth = 120f;
    private const float WarningIconSize = 14f;

    private static CanvasGroup BuildSignupPanel(Transform parent, out Button linkLogin)
    {
        CanvasGroup panel = CreatePanel("Panel_Signup", parent, 120f, 150f);
        Transform root = panel.transform;

        CreateText("Tag", root, "// ZOMBIE OUTBREAK", _fontMono, 14f, Accent, TextAlignmentOptions.TopLeft);

        AddSpacer(root, 10f);
        CreateText("Title", root, "3D FPS", _fontTitle, 64f, TextPrimary, TextAlignmentOptions.TopLeft,
            FontStyles.Bold);

        AddSpacer(root, 40f);
        CreateHeader(root, "회원가입", "02 / REGISTER");

        AddSpacer(root, 14f);
        CreateDivider(root);

        // 아이디 + 중복 확인 버튼 (OnClick은 내일 연결)
        AddSpacer(root, 28f);
        CreateLabel(root, "Label_Id", "아이디");
        AddSpacer(root, 8f);
        CreateIdRow(root);

        // 비밀번호 + 규칙 안내 문구 자리
        AddSpacer(root, 20f);
        CreateLabel(root, "Label_Password", "비밀번호");
        AddSpacer(root, 8f);
        CreateInputField(root, "Input_Password", "비밀번호를 입력하세요", true);
        AddSpacer(root, 8f);
        CreateText("Hint_Password", root, "[비밀번호 규칙 안내 문구]", _fontKrRegular, 13f, TextMuted,
            TextAlignmentOptions.TopLeft);

        // 비밀번호 확인 + 오류 문구 자리 (평소 비활성, 오류 표시는 내일 연결)
        AddSpacer(root, 20f);
        CreateLabel(root, "Label_PasswordConfirm", "비밀번호 확인");
        AddSpacer(root, 8f);
        CreateInputField(root, "Input_PasswordConfirm", "비밀번호를 다시 입력하세요", true);
        CreateErrorMessage(root, "Error_PasswordMismatch", "비밀번호가 일치하지 않습니다");

        // 가입하기 버튼 (OnClick은 내일 연결)
        AddSpacer(root, 28f);
        CreatePrimaryButton(root, "Button_Signup", "가입하기");

        AddSpacer(root, 26f);
        linkLogin = CreateFooter(root, "이미 계정이 있나요?", "Link_Login", "로그인");

        return panel;
    }

    // 아이디 입력칸(남은 폭 전부) + 테두리만 있는 중복 확인 버튼(폭 120)
    private static void CreateIdRow(Transform parent)
    {
        RectTransform row = CreateRect("Row_Id", parent);
        SetLayoutSize(row.gameObject, -1f, InputHeight);
        AddHorizontal(row.gameObject, TextAnchor.MiddleLeft, 8f, true);

        TMP_InputField input = CreateInputField(row, "Input_Id", "아이디를 입력하세요", false);
        input.GetComponent<LayoutElement>().flexibleWidth = 1f;

        Button checkButton = CreateOutlineButton(row, "Button_CheckId", "중복 확인", 15f);
        SetLayoutSize(checkButton.gameObject, CheckButtonWidth, InputHeight);
    }

    // ⚠ 아이콘(폰트에 글리프가 없어 sprite Image) + 오류 문구. 비활성 상태라 레이아웃 공간을 차지하지 않는다.
    private static void CreateErrorMessage(Transform parent, string name, string message)
    {
        RectTransform row = CreateRect(name, parent);
        SetLayoutSize(row.gameObject, -1f, 26f);
        HorizontalLayoutGroup layout = AddHorizontal(row.gameObject, TextAnchor.LowerLeft, 6f);
        layout.padding = new RectOffset(0, 0, 0, 0);

        Image icon = CreateImage("Icon_Warning", row, _spriteWarning, ErrorText, false);
        icon.preserveAspect = true;
        SetLayoutSize(icon.gameObject, WarningIconSize, WarningIconSize);

        CreateText("Text_Error", row, message, _fontKrRegular, 13f, ErrorText, TextAlignmentOptions.Left);

        row.gameObject.SetActive(false);
    }
}
