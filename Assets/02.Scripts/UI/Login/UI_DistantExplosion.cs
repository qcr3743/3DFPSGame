using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 멀리서 터지는 폭발 연출. 랜덤 간격으로 지평선 위치 중 하나에 주황 섬광을 띄우고 배경을 살짝 흔든다.
public class UI_DistantExplosion : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Image _flash; // 비어 있으면 같은 오브젝트의 Image

    [SerializeField] private RectTransform _shakeTarget; // 흔들 배경 루트 (BG)

    [Header("폭발 위치 (1920x1080 좌상단 기준 px)")]
    [SerializeField] private Vector2 _referenceResolution = new Vector2(1920f, 1080f);

    [SerializeField]
    private Vector2[] _positions =
    {
        new Vector2(1395f, 440f),
        new Vector2(1555f, 430f),
        new Vector2(1655f, 405f),
    };

    [Header("간격")]
    [SerializeField] private float _minInterval = 4f;

    [SerializeField] private float _maxInterval = 9f;

    [Header("섬광")]
    [SerializeField] private float _flashDuration = 0.6f;

    [SerializeField] private float _peakAlpha = 0.7f;
    [SerializeField, Range(0.05f, 0.95f)] private float _peakTime = 0.2f; // 전체 시간 중 가장 밝은 지점 (빨리 켜지고 천천히 사라짐)
    [SerializeField] private float _sizeJitter = 0.15f; // 폭발마다 크기를 ±15% 다르게

    [Header("흔들림")]
    [SerializeField] private float _shakeDuration = 0.3f;

    [SerializeField] private float _minShake = 2f;
    [SerializeField] private float _maxShake = 4f;

    [Header("효과음 (비어 있으면 무시)")]
    [SerializeField] private AudioSource _audioSource;

    [SerializeField] private AudioClip _explosionClip;

    private RectTransform _flashRect;
    private Vector2 _flashBaseSize;
    private Color _flashBaseColor;
    private Vector2 _shakeBasePosition;

    private void Awake()
    {
        if (_flash == null)
        {
            _flash = GetComponent<Image>();
        }

        _flashRect = _flash.rectTransform;
        _flashBaseSize = _flashRect.sizeDelta;
        _flashBaseColor = _flash.color;

        if (_shakeTarget != null)
        {
            _shakeBasePosition = _shakeTarget.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        SetFlashAlpha(0f);
        StartCoroutine(Loop_Coroutine());
    }

    private void OnDisable()
    {
        // 연출 도중 꺼져도 섬광과 흔들림이 남지 않게 정리한다.
        StopAllCoroutines();
        SetFlashAlpha(0f);
        if (_shakeTarget != null)
        {
            _shakeTarget.anchoredPosition = _shakeBasePosition;
        }
    }

    private IEnumerator Loop_Coroutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(_minInterval, _maxInterval));

            if (_positions == null || _positions.Length == 0)
            {
                continue;
            }

            PlaceFlash(_positions[Random.Range(0, _positions.Length)]);

            if (_audioSource != null && _explosionClip != null)
            {
                _audioSource.PlayOneShot(_explosionClip);
            }

            if (_shakeTarget != null)
            {
                StartCoroutine(Shake_Coroutine());
            }

            yield return Flash_Coroutine();
        }
    }

    // 좌상단 px 좌표를 부모 기준 정규화 앵커로 바꿔 배치한다 (배경이 늘어나도 같은 지점에 붙음).
    private void PlaceFlash(Vector2 position)
    {
        Vector2 anchor = new Vector2(position.x / _referenceResolution.x, 1f - position.y / _referenceResolution.y);
        _flashRect.anchorMin = anchor;
        _flashRect.anchorMax = anchor;
        _flashRect.anchoredPosition = Vector2.zero;
        _flashRect.sizeDelta = _flashBaseSize * Random.Range(1f - _sizeJitter, 1f + _sizeJitter);
    }

    // alpha 0 → peak → 0
    private IEnumerator Flash_Coroutine()
    {
        float time = 0f;
        while (time < _flashDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / _flashDuration);

            float alpha = t < _peakTime
                ? Mathf.Lerp(0f, _peakAlpha, t / _peakTime)
                : Mathf.Lerp(_peakAlpha, 0f, (t - _peakTime) / (1f - _peakTime));
            SetFlashAlpha(alpha);

            yield return null;
        }

        SetFlashAlpha(0f);
    }

    // 시간이 지날수록 흔들림 폭이 줄어든다.
    private IEnumerator Shake_Coroutine()
    {
        float strength = Random.Range(_minShake, _maxShake);
        float time = 0f;
        while (time < _shakeDuration)
        {
            time += Time.unscaledDeltaTime;
            float fade = 1f - Mathf.Clamp01(time / _shakeDuration);
            _shakeTarget.anchoredPosition = _shakeBasePosition + Random.insideUnitCircle * strength * fade;
            yield return null;
        }

        _shakeTarget.anchoredPosition = _shakeBasePosition;
    }

    private void SetFlashAlpha(float alpha)
    {
        if (_flash == null)
        {
            return;
        }

        Color color = _flashBaseColor;
        color.a = alpha;
        _flash.color = color;
    }
}