using System.Collections;
using TMPro;
using UnityEngine;

// 씬 시작 연출: 검은 화면 → CONNECTING... → 검은 화면 페이드 아웃 → HUD 선 그어짐 → 로그인 패널 등장.
// 아무 키(마우스 버튼 포함)나 누르면 바로 마지막 상태로 건너뛴다.
public class UI_IntroSequence : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CanvasGroup _fxOverlay;        // 검은 화면 (평소 alpha 0)
    [SerializeField] private TMP_Text _connectingText;
    [SerializeField] private RectTransform[] _hudLines;     // 왼쪽 pivot 기준으로 가로 scale 0 → 1
    [SerializeField] private CanvasGroup _loginPanel;

    [Header("시작")]
    [SerializeField] private float _startDelay = 0.3f;          // 시작 전 검은 화면만 유지하는 시간 (첫 프레임 로딩 멈춤 흡수)
    [SerializeField] private float _skipLockDuration = 0.5f;    // 시작 직후 이 시간 동안은 키 입력으로 스킵하지 않음 (녹화 시작 키 등)
    [SerializeField] private float _maxDeltaTime = 1f / 30f;    // 한 프레임에 진행할 최대 시간 (멈춤이 생겨도 연출을 건너뛰지 않게)

    [Header("연출 시간")]
    [SerializeField] private float _connectingDuration = 1f;
    [SerializeField] private float _dotInterval = 0.25f;     // 점이 하나씩 늘어나는 간격
    [SerializeField] private float _blackFadeDuration = 0.5f;
    [SerializeField] private float _lineDrawDuration = 0.4f;
    [SerializeField] private float _panelFadeDuration = 0.35f;
    [SerializeField] private float _panelSlideDistance = 24f;

    private const string ConnectingBaseText = "CONNECTING";

    private RectTransform _loginPanelRect;
    private Vector2 _loginPanelBasePosition;
    private Coroutine _introCoroutine;
    private bool _isPlaying;
    private float _playTime;   // 인트로 시작 후 지난 시간 (스킵 잠금 판단용)

    // 인트로가 재생 중인지 (UI_ShootCatcher가 사격을 막는 데 쓴다)
    public bool IsPlaying => _isPlaying;

    // unscaledDeltaTime은 Maximum Delta Time으로 잘리지 않아서, 씬 로딩·녹화 시작 같은 멈춤이 있으면
    // 그 시간이 한 프레임에 통째로 들어온다. 연출용으로는 상한을 둬서 멈춤 뒤에도 처음부터 보이게 한다.
    private float DeltaTime => Mathf.Min(Time.unscaledDeltaTime, _maxDeltaTime);

    private void Awake()
    {
        _loginPanelRect = _loginPanel.transform as RectTransform;
        _loginPanelBasePosition = _loginPanelRect.anchoredPosition;
    }

    private void Start()
    {
        _introCoroutine = StartCoroutine(Intro_Coroutine());
    }

    private void Update()
    {
        if (!_isPlaying)
        {
            return;
        }

        _playTime += DeltaTime;
        if (_playTime >= _skipLockDuration && Input.anyKeyDown)
        {
            Skip();
        }
    }

    private void Skip()
    {
        if (_introCoroutine != null)
        {
            StopCoroutine(_introCoroutine);
            _introCoroutine = null;
        }

        ApplyFinalState();
    }

    private IEnumerator Intro_Coroutine()
    {
        _isPlaying = true;
        _playTime = 0f;
        ApplyInitialState();

        // 0. 시작 대기: 검은 화면만 유지 (글자는 아직 숨김)
        if (_connectingText != null)
        {
            _connectingText.gameObject.SetActive(false);
        }

        float time = 0f;
        while (time < _startDelay)
        {
            time += DeltaTime;
            yield return null;
        }

        if (_connectingText != null)
        {
            _connectingText.gameObject.SetActive(true);
        }

        // 1. CONNECTING + 점 0~3개 반복
        time = 0f;
        while (time < _connectingDuration)
        {
            int dotCount = _dotInterval > 0f ? (int)(time / _dotInterval) % 4 : 3;
            if (_connectingText != null)
            {
                _connectingText.text = ConnectingBaseText + new string('.', dotCount);
            }

            time += DeltaTime;
            yield return null;
        }

        if (_connectingText != null)
        {
            _connectingText.gameObject.SetActive(false);
        }

        // 2. 검은 화면 페이드 아웃
        time = 0f;
        while (time < _blackFadeDuration)
        {
            time += DeltaTime;
            _fxOverlay.alpha = 1f - Mathf.Clamp01(time / _blackFadeDuration);
            yield return null;
        }
        _fxOverlay.alpha = 0f;
        _fxOverlay.blocksRaycasts = false;

        // 3. HUD 선 그어짐
        time = 0f;
        while (time < _lineDrawDuration)
        {
            time += DeltaTime;
            SetLineScale(EaseOut(Mathf.Clamp01(time / _lineDrawDuration)));
            yield return null;
        }
        SetLineScale(1f);

        // 4. 로그인 패널 페이드 + 왼쪽에서 슬라이드 인
        time = 0f;
        while (time < _panelFadeDuration)
        {
            time += DeltaTime;
            float eased = EaseOut(Mathf.Clamp01(time / _panelFadeDuration));
            _loginPanel.alpha = eased;
            _loginPanelRect.anchoredPosition = _loginPanelBasePosition + new Vector2(-_panelSlideDistance * (1f - eased), 0f);
            yield return null;
        }

        _introCoroutine = null;
        ApplyFinalState();
    }

    private void ApplyInitialState()
    {
        _fxOverlay.alpha = 1f;
        _fxOverlay.blocksRaycasts = true;   // 연출 중에는 뒤쪽 UI 클릭을 막는다

        if (_connectingText != null)
        {
            _connectingText.gameObject.SetActive(true);
            _connectingText.text = ConnectingBaseText;
        }

        SetLineScale(0f);

        _loginPanel.alpha = 0f;
        _loginPanel.interactable = false;
        _loginPanelRect.anchoredPosition = _loginPanelBasePosition + new Vector2(-_panelSlideDistance, 0f);
    }

    private void ApplyFinalState()
    {
        _isPlaying = false;

        _fxOverlay.alpha = 0f;
        _fxOverlay.blocksRaycasts = false;

        if (_connectingText != null)
        {
            _connectingText.gameObject.SetActive(false);
        }

        SetLineScale(1f);

        _loginPanel.alpha = 1f;
        _loginPanel.interactable = true;
        _loginPanelRect.anchoredPosition = _loginPanelBasePosition;
    }

    private void SetLineScale(float scaleX)
    {
        if (_hudLines == null)
        {
            return;
        }

        foreach (RectTransform line in _hudLines)
        {
            if (line != null)
            {
                line.localScale = new Vector3(scaleX, 1f, 1f);
            }
        }
    }

    private static float EaseOut(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
}
