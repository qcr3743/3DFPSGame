using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel_Login (x 120, y 200, 폭 480). 위에서 아래로 Vertical Layout, "위 간격"은 Spacer로 맞춘다.
public static partial class LoginSceneBuilder
{
    private static CanvasGroup BuildLoginPanel(Transform parent, out Button linkSignup)
    {
        CanvasGroup panel = CreatePanel("Panel_Login", parent, 120f, 200f);
        Transform root = panel.transform;

        CreateText("Tag", root, "// ZOMBIE OUTBREAK", _fontMono, 14f, Accent, TextAlignmentOptions.TopLeft);

        AddSpacer(root, 10f);
        CreateText("Title", root, "3D FPS", _fontTitle, 112f, TextPrimary, TextAlignmentOptions.TopLeft,
            FontStyles.Bold);

        AddSpacer(root, 14f);
        CreateText("Subtitle", root, "SURVIVE THE QUARANTINE ZONE", _fontMono, 13f, TextMuted,
            TextAlignmentOptions.TopLeft);

        AddSpacer(root, 72f);
        CreateHeader(root, "로그인", "01 / SIGN IN");

        AddSpacer(root, 14f);
        CreateDivider(root);

        // 아이디
        AddSpacer(root, 28f);
        CreateLabel(root, "Label_Id", "아이디");
        AddSpacer(root, 8f);
        CreateInputField(root, "Input_Id", "아이디를 입력하세요", false);

        // 비밀번호
        AddSpacer(root, 20f);
        CreateLabel(root, "Label_Password", "비밀번호");
        AddSpacer(root, 8f);
        CreateInputField(root, "Input_Password", "비밀번호를 입력하세요", true);
        // 로그인 오류 / 가입 완료 안내 문구 자리 (평소 비활성, UI_Login이 문구를 채워 켠다)
        CreateErrorMessage(root, "Error_Login", string.Empty);

        // 아이디 저장
        AddSpacer(root, 20f);
        CreateToggle(root, "Toggle_SaveId", "아이디 저장");

        // 로그인 버튼 (OnClick은 BuildCanvas에서 UI_Login.Login에 연결)
        AddSpacer(root, 28f);
        CreatePrimaryButton(root, "Button_Login", "로그인");

        AddSpacer(root, 26f);
        linkSignup = CreateFooter(root, "계정이 없나요?", "Link_Signup", "회원가입");

        return panel;
    }
}
