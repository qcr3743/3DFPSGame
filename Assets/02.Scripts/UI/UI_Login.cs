using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 로그인/회원가입 기능 로직. 입력 검사와 안내 문구만 담당한다.
// 패널 전환 연출은 UI_PanelSwitcher가 맡는다. (로그인 패널과 회원가입 패널은 별도 오브젝트)
// 씬 연결은 LoginSceneBuilder(Tools > Login > Build Login UI)가 한다.
public class UI_Login : MonoBehaviour
{
    [Header("패널 전환")]
    [SerializeField] private UI_PanelSwitcher _panelSwitcher;

    [Header("로그인 패널")]
    [SerializeField] private TMP_InputField _loginIdInput;

    [SerializeField] private TMP_InputField _loginPasswordInput;
    [SerializeField] private GameObject _loginMessage; // Error_Login 줄 (평소 비활성)
    [SerializeField] private TMP_Text _loginMessageText;
    [SerializeField] private Image _loginMessageIcon; // ⚠ 아이콘. 가입 완료 안내 때는 숨긴다.

    [Header("회원가입 패널")]
    [SerializeField] private TMP_InputField _signupIdInput;

    [SerializeField] private TMP_InputField _signupPasswordInput;
    [SerializeField] private TMP_InputField _signupPasswordConfirmInput;
    [SerializeField] private GameObject _passwordMismatchMessage; // Error_PasswordMismatch 줄 (비밀번호 확인 칸 바로 아래)
    [SerializeField] private GameObject _signupMessage; // Error_Signup 줄 (가입하기 버튼 위)
    [SerializeField] private TMP_Text _signupMessageText;

    [Header("안내 문구 색")]
    [SerializeField] private Color _errorColor = new Color(1f, 0.541f, 0.439f); // 오류 (#FF8A70)

    [SerializeField] private Color _noticeColor = new Color(0.62f, 0.902f, 0.18f); // 안내 (#9EE62E)

    [Header("기타")]
    [SerializeField] private string _gameSceneName = "GameScene"; // 로그인하면 불러올 씬

    private PlayerPrefsAccountRepository _repository = new PlayerPrefsAccountRepository();

    // 로그인/회원가입 링크 버튼 OnClick에 연결 (패널을 오갈 때 지난 안내 문구를 지운다)
    public void ClearMessages()
    {
        _loginMessage.SetActive(false);
        _signupMessage.SetActive(false);
        _passwordMismatchMessage.SetActive(false);
    }

    // 로그인 버튼 OnClick에 연결
    public void Login()
    {
        string id = _loginIdInput.text;
        string password = _loginPasswordInput.text;

        // 아이디나 비밀번호 중 하나라도 비어 있으면 안내 문구만 띄우고 씬을 넘기지 않는다.
        if (string.IsNullOrEmpty(id))
        {
            ShowLoginMessage("아이디를 입력하세요.", true);
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowLoginMessage("비밀번호를 입력하세요.", true);
            return;
        }

        if (_repository.CheckPassword(id, password))
        {
            SceneManager.LoadScene(_gameSceneName);
        }
        else
        {
            _loginMessageText.text = "아이디와 비밀번호를 확인해주세요.";
            return;
        }
    }

    // 가입하기 버튼 OnClick에 연결
    public void Register()
    {
        string id = _signupIdInput.text;
        string password = _signupPasswordInput.text;
        string passwordConfirm = _signupPasswordConfirmInput.text;

        // 이전에 띄운 오류는 먼저 지우고 다시 검사한다.
        _signupMessage.SetActive(false);
        _passwordMismatchMessage.SetActive(false);

        // Todo: ID와 비밀번호 규칙
        // ID는 이메일
        // 비밀번호는 영어, 특수문자, 숫자만 가능, 특수문자, 영어대문자를 각각 하나 이상 포함한 7~15글자
        // 규칙이 여기서만 쓰일까?

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            _signupMessageText.text = "아이디와 비밀번호를 입력해 주세요.";
            _signupMessage.SetActive(true);
            return;
        }

        // 비밀번호 불일치는 씬에 미리 만들어 둔 전용 문구(비밀번호 확인 칸 아래)를 켠다.
        if (password != passwordConfirm)
        {
            _passwordMismatchMessage.SetActive(true);
            return;
        }

        // Todo: 중복 ID 검사
        if (_repository.Exists(id))
        {
            _loginMessageText.text = "이미 존재하는 아이디입니다.";
            return;
        }

        // Todo: 실제 회원가입 데이터 저장
        _repository.Register(id, password);

        // 가입 성공: 회원가입 칸은 비우고, 로그인 칸에 방금 만든 아이디를 넣어 둔 채 로그인 패널로 돌아간다.
        _signupIdInput.text = string.Empty;
        _signupPasswordInput.text = string.Empty;
        _signupPasswordConfirmInput.text = string.Empty;

        _loginIdInput.text = id;
        _loginPasswordInput.text = string.Empty;
        ShowLoginMessage("가입했습니다. 로그인해 주세요.", false);

        _panelSwitcher.ShowLogin();
    }

    // 로그인 패널 안내 줄: 오류면 ⚠ 아이콘 + 오류 색, 안내면 아이콘 없이 안내 색
    private void ShowLoginMessage(string message, bool isError)
    {
        _loginMessageText.text = message;
        _loginMessageText.color = isError ? _errorColor : _noticeColor;
        _loginMessageIcon.gameObject.SetActive(isError);
        _loginMessage.SetActive(true);
    }
}