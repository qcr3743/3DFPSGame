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

        if (Input.anyKeyDown)
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
        ApplyInitialState();

        // 1. CONNECTING + 점 0~3개 반복
        float time = 0f;
        while (time < _connectingDuration)
        {
            int dotCount = _dotInterval > 0f ? (int)(time / _dotInterval) % 4 : 3;
            if (_connectingText != null)
            {
                _connectingText.text = ConnectingBaseText + new string('.', dotCount);
            }

            time += Time.unscaledDeltaTime;
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
            time += Time.unscaledDeltaTime;
            _fxOverlay.alpha = 1f - Mathf.Clamp01(time / _blackFadeDuration);
            yield return null;
        }
        _fxOverlay.alpha = 0f;
        _fxOverlay.blocksRaycasts = false;

        // 3. HUD 선 그어짐
        time = 0f;
        while (time < _lineDrawDuration)
        {
            time += Time.unscaledDeltaTime;
            SetLineScale(EaseOut(Mathf.Clamp01(time / _lineDrawDuration)));
            yield return null;
        }
        SetLineScale(1f);

        // 4. 로그인 패널 페이드 + 왼쪽에서 슬라이드 인
        time = 0f;
        while (time < _panelFadeDuration)
        {
            time += Time.unscaledDeltaTime;
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
