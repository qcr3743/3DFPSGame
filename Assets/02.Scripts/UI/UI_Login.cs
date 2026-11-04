using TMPro;
using UnityEngine;
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

    // 회원가입 규칙 (정규식은 각 Specification 클래스 안에 있다)
    private AccountIdSpecification _idSpecification = new AccountIdSpecification();
    private AccountPasswordSpecification _passwordSpecification = new AccountPasswordSpecification();

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
            SceneLoader.Load(_gameSceneName);
        }
        else
        {
            ShowLoginMessage("아이디와 비밀번호를 확인해주세요.", true);
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

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            _signupMessageText.text = "아이디와 비밀번호를 입력해 주세요.";
            _signupMessage.SetActive(true);
            return;
        }

        // 정규표현식(정규식)
        // 문자열에서 특정한 규칙이나 패턴을 찾기 위해 사용하는 문법
        // - 데이터 검증 (이메일 형식, 주민번호 형식, 비밀번호 형식)
        // - 문자열 검색 (ToDo라는게 있나?, 한국어가 있나?)
        // - 문자열 치환 (규칙에 맞는 문자열을 찾아서 다른 문자열로 변경)
        // - 웹 크롤링 & 파싱

        // ───── ID 규칙: 이메일 형식만 허용 ─────

        if (!_idSpecification.IsSatisfiedBy(id))
        {
            _signupMessageText.text = _idSpecification.ErrorMessage;
            _signupMessage.SetActive(true);
            return;
        }

        // ───── 비밀번호 규칙: 대문자, 소문자, 숫자, 기호를 모두 포함한 8~30 글자 ─────
        // ErrorMessage에는 걸린 규칙(길이/글자 종류/조합)에 맞는 문구가 들어 있다.

        if (!_passwordSpecification.IsSatisfiedBy(password))
        {
            _signupMessageText.text = _passwordSpecification.ErrorMessage;
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
            _signupMessageText.text = "이미 존재하는 아이디입니다.";
            _signupMessage.SetActive(true);
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