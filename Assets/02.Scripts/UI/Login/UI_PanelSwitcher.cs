using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 로그인 ↔ 회원가입 패널 전환 연출.
// 0.12초 신호 간섭(FX_Overlay 깜빡임 + 패널 x 흔들림 + 색수차) → 패널 교체 → 새 패널 페이드 + 슬라이드 인.
// 연출만 담당한다. 로그인/회원가입 기능 로직은 여기에 넣지 않는다.
public class UI_PanelSwitcher : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private CanvasGroup _loginPanel;
    [SerializeField] private CanvasGroup _signupPanel;

    [Header("신호 간섭")]
    [SerializeField] private CanvasGroup _fxOverlay;
    [SerializeField] private Volume _volume;                      // 색수차를 잠깐 올릴 Global Volume
    [SerializeField] private float _glitchDuration = 0.12f;
    [SerializeField] private float _glitchOverlayMaxAlpha = 0.6f;   // 깜빡임 때 검은 화면의 최대 alpha
    [SerializeField] private float _glitchShakeDistance = 8f;       // 패널 x 흔들림 폭(px)
    [SerializeField] private float _glitchChromaticIntensity = 0.6f;

    [Header("새 패널 등장")]
    [SerializeField] private float _fadeInDuration = 0.25f;
    [SerializeField] private float _slideInDistance = 24f;          // 왼쪽에서 밀려 들어오는 거리(px)

    private RectTransform _loginRect;
    private RectTransform _signupRect;
    private Vector2 _loginBasePosition;
    private Vector2 _signupBasePosition;
    private ChromaticAberration _chromatic;
    private bool _isSwitching;

    private void Awake()
    {
        _loginRect = _loginPanel.transform as RectTransform;
        _signupRect = _signupPanel.transform as RectTransform;
        _loginBasePosition = _loginRect.anchoredPosition;
        _signupBasePosition = _signupRect.anchoredPosition;

        // volume.profile은 실행 중에 프로필 복사본을 만들어 돌려주므로 원본 에셋(.asset)은 바뀌지 않는다.
        if (_volume != null && _volume.profile != null && _volume.profile.TryGet(out ChromaticAberration chromatic))
        {
            _chromatic = chromatic;
        }
    }

    private void OnDisable()
    {
        // 전환 도중 꺼지면 코루틴이 멈추므로 화면 효과가 남지 않게 정리한다.
        _isSwitching = false;
        SetChromatic(0f);
        if (_fxOverlay != null)
        {
            _fxOverlay.alpha = 0f;
        }
    }

    // 회원가입 패널의 "로그인" 링크 버튼 OnClick에 연결
    public void ShowLogin()
    {
        Switch(_signupPanel, _signupRect, _signupBasePosition, _loginPanel, _loginRect, _loginBasePosition);
    }

    // 로그인 패널의 "회원가입" 링크 버튼 OnClick에 연결
    public void ShowSignup()
    {
        Switch(_loginPanel, _loginRect, _loginBasePosition, _signupPanel, _signupRect, _signupBasePosition);
    }

    private void Switch(CanvasGroup from, RectTransform fromRect, Vector2 fromBase,
                        CanvasGroup to, RectTransform toRect, Vector2 toBase)
    {
        // 전환 중이거나 이미 목표 패널이 보이는 중이면 무시한다.
        if (_isSwitching || (to.gameObject.activeSelf && !from.gameObject.activeSelf))
        {
            return;
        }

        StartCoroutine(Switch_Coroutine(from, fromRect, fromBase, to, toRect, toBase));
    }

    private IEnumerator Switch_Coroutine(CanvasGroup from, RectTransform fromRect, Vector2 fromBase,
                                         CanvasGroup to, RectTransform toRect, Vector2 toBase)
    {
        _isSwitching = true;
        from.interactable = false;

        // 숨겨질 패널의 입력칸에 포커스가 남아 있지 않게 선택 해제
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // 1. 신호 간섭: 매 프레임 무작위로 깜빡이고 흔들린다.
        float time = 0f;
        while (time < _glitchDuration)
        {
            time += Time.unscaledDeltaTime;

            if (_fxOverlay != null)
            {
                _fxOverlay.alpha = Random.Range(0f, _glitchOverlayMaxAlpha);
            }
            fromRect.anchoredPosition = fromBase + new Vector2(Random.Range(-_glitchShakeDistance, _glitchShakeDistance), 0f);
            SetChromatic(_glitchChromaticIntensity);

            yield return null;
        }

        if (_fxOverlay != null)
        {
            _fxOverlay.alpha = 0f;
        }
        SetChromatic(0f);

        // 2. 패널 교체 (숨긴 패널은 다음에 다시 보일 때를 위해 원래 상태로 돌려 둔다)
        fromRect.anchoredPosition = fromBase;
        from.alpha = 1f;
        from.interactable = true;
        from.gameObject.SetActive(false);

        to.gameObject.SetActive(true);
        to.alpha = 0f;
        to.interactable = false;

        // 3. 새 패널: 페이드 + 왼쪽에서 슬라이드 인 (ease-out)
        time = 0f;
        while (time < _fadeInDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / _fadeInDuration);
            float eased = 1f - (1f - t) * (1f - t);

            to.alpha = eased;
            toRect.anchoredPosition = toBase + new Vector2(-_slideInDistance * (1f - eased), 0f);

            yield return null;
        }

        to.alpha = 1f;
        toRect.anchoredPosition = toBase;
        to.interactable = true;
        _isSwitching = false;
    }

    private void SetChromatic(float intensity)
    {
        if (_chromatic != null)
        {
            _chromatic.intensity.value = intensity;
        }
    }
}
