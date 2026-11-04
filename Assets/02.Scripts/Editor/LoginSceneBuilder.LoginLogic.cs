using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

// UI_Login(로그인/회원가입 기능 로직)을 Canvas_Login에 붙이고 두 패널의 입력칸·문구·버튼과 연결한다.
// 패널을 다 만든 뒤 BuildCanvas에서 호출한다. 이름으로 찾으므로 패널 쪽 오브젝트 이름을 바꾸면 여기도 바꿔야 한다.
public static partial class LoginSceneBuilder
{
    private static void BuildLoginLogic(GameObject canvasGo, UI_PanelSwitcher switcher,
                                        Transform loginPanel, Transform signupPanel,
                                        Button linkSignup, Button linkLogin)
    {
        var login = canvasGo.AddComponent<UI_Login>();
        SetField(login, "_panelSwitcher", switcher);

        // 로그인 패널
        Transform loginError = loginPanel.Find("Error_Login");
        SetField(login, "_loginIdInput", FindComponent<TMP_InputField>(loginPanel, "Input_Id"));
        SetField(login, "_loginPasswordInput", FindComponent<TMP_InputField>(loginPanel, "Input_Password"));
        SetField(login, "_loginMessage", loginError.gameObject);
        SetField(login, "_loginMessageText", FindComponent<TMP_Text>(loginError, "Text_Error"));
        SetField(login, "_loginMessageIcon", FindComponent<Image>(loginError, "Icon_Warning"));

        // 회원가입 패널
        Transform signupError = signupPanel.Find("Error_Signup");
        SetField(login, "_signupIdInput", FindComponent<TMP_InputField>(signupPanel, "Row_Id/Input_Id"));
        SetField(login, "_signupPasswordInput", FindComponent<TMP_InputField>(signupPanel, "Input_Password"));
        SetField(login, "_signupPasswordConfirmInput", FindComponent<TMP_InputField>(signupPanel, "Input_PasswordConfirm"));
        SetField(login, "_passwordMismatchMessage", signupPanel.Find("Error_PasswordMismatch").gameObject);
        SetField(login, "_signupMessage", signupError.gameObject);
        SetField(login, "_signupMessageText", FindComponent<TMP_Text>(signupError, "Text_Error"));

        SetColorField(login, "_errorColor", ErrorText);
        SetColorField(login, "_noticeColor", Accent);

        // 버튼 OnClick
        UnityEventTools.AddPersistentListener(FindComponent<Button>(loginPanel, "Button_Login").onClick, login.Login);
        UnityEventTools.AddPersistentListener(FindComponent<Button>(signupPanel, "Button_Signup").onClick, login.Register);

        // 패널을 오갈 때 지난 안내 문구를 지운다 (패널 전환 자체는 위에서 연결한 UI_PanelSwitcher가 한다)
        UnityEventTools.AddPersistentListener(linkSignup.onClick, login.ClearMessages);
        UnityEventTools.AddPersistentListener(linkLogin.onClick, login.ClearMessages);
    }

    private static T FindComponent<T>(Transform parent, string path) where T : Component
    {
        Transform child = parent.Find(path);
        if (child == null)
        {
            Debug.LogError($"[LoginSceneBuilder] {parent.name}/{path}를 찾을 수 없습니다.");
            return null;
        }
        return child.GetComponent<T>();
    }
}
