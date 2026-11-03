using UnityEngine;
using UnityEngine.UI;

// 총구 불꽃: 별 모양 섬광을 랜덤 회전으로 아주 짧게(0.05초) 보여 주고, 주변 주황 glow는 0.08초 동안 페이드.
// 화면 오른쪽 아래 밖에서 쏘는 느낌을 주기 위해 UI_ShootCatcher가 총구 위치를 넘겨 준다.
public class UI_MuzzleFlash : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RectTransform _root;     // 비어 있으면 자기 자신 (앵커·피벗 가운데)
    [SerializeField] private Graphic _flash;          // 별 모양 섬광
    [SerializeField] private Graphic _glow;           // 주변 주황 glow

    [Header("섬광")]
    [SerializeField] private float _flashDuration = 0.05f;
    [SerializeField] private float _flashAlpha = 1f;
    [SerializeField] private float _flashScaleJitter = 0.2f;   // 발사마다 크기 ±20%

    [Header("glow")]
    [SerializeField] private float _glowDuration = 0.08f;
    [SerializeField] private float _glowAlpha = 0.35f;

    private float _flashTimer;
    private float _glowTimer;

    private void Awake()
    {
        if (_root == null)
        {
            _root = transform as RectTransform;
        }
    }

    private void OnEnable()
    {
        _flashTimer = 0f;
        _glowTimer = 0f;
        SetAlpha(_flash, 0f);
        SetAlpha(_glow, 0f);
    }

    public void Play(Vector2 localPosition)
    {
        _root.anchoredPosition = localPosition;

        if (_flash != null)
        {
            float scale = Random.Range(1f - _flashScaleJitter, 1f + _flashScaleJitter);
            _flash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            _flash.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        _flashTimer = _flashDuration;
        _glowTimer = _glowDuration;
        SetAlpha(_flash, _flashAlpha);
        SetAlpha(_glow, _glowAlpha);
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        // 섬광은 페이드 없이 켜졌다가 바로 꺼진다 (짧고 날카롭게).
        if (_flashTimer > 0f)
        {
            _flashTimer -= deltaTime;
            if (_flashTimer <= 0f)
            {
                SetAlpha(_flash, 0f);
            }
        }

        if (_glowTimer > 0f)
        {
            _glowTimer = Mathf.Max(0f, _glowTimer - deltaTime);
            float t = _glowDuration > 0f ? _glowTimer / _glowDuration : 0f;
            SetAlpha(_glow, _glowAlpha * t);
        }
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}
