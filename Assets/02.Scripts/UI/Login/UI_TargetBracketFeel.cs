using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 조준 표시(TargetBracket)의 반응과 HOSTILE 카운트.
//  - 평소: 네 모서리가 2초 주기로 안쪽으로 4px 오므렸다 펴짐 (숨쉬기)
//  - 명중: 모서리 흰색 번쩍 + 안쪽으로 탁 오므라들었다 복귀, 숫자는 1.4배 → 1배
//  - 마지막 처치: 모서리 녹색 + "AREA CLEAR" 표시 후 HOSTILE ×3으로 리셋
public class UI_TargetBracketFeel : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RectTransform[] _corners;     // 각 모서리 루트 (앵커가 해당 모서리)
    [SerializeField] private Graphic[] _cornerGraphics;    // 모서리 선 (색을 바꿀 대상)
    [SerializeField] private TMP_Text _hostileText;

    [Header("숨쉬기")]
    [SerializeField] private float _breathDepth = 4f;      // 안쪽으로 오므리는 거리(px)
    [SerializeField] private float _breathPeriod = 2f;

    [Header("명중 반응")]
    [SerializeField] private Color _hitFlashColor = Color.white;
    [SerializeField] private float _hitFlashDuration = 0.08f;
    [SerializeField] private float _pinchDepth = 6f;
    [SerializeField] private float _pinchDuration = 0.15f;
    [SerializeField] private float _numberPunchScale = 1.4f;
    [SerializeField] private float _numberPunchDuration = 0.2f;

    [Header("목표")]
    [SerializeField] private int _hostileCount = 3;
    [SerializeField] private string _hostileLabel = "HOSTILE ×";
    [SerializeField] private string _clearText = "AREA CLEAR";
    [SerializeField] private float _clearDuration = 1.2f;
    [SerializeField] private Color _clearColor = new Color(0.3f, 1f, 0.48f, 1f);   // 처치 완료 녹색

    private Color _baseColor;
    private Vector2[] _cornerBasePositions;
    private Vector2[] _cornerInwardDirections;

    private int _remaining;
    private float _time;
    private float _hitFlashTimer;
    private float _pinchTimer;
    private float _punchTimer;
    private float _clearTimer;

    public bool IsClearing => _clearTimer > 0f;

    private void Awake()
    {
        _baseColor = _cornerGraphics != null && _cornerGraphics.Length > 0 && _cornerGraphics[0] != null
            ? _cornerGraphics[0].color
            : Color.white;

        int count = _corners != null ? _corners.Length : 0;
        _cornerBasePositions = new Vector2[count];
        _cornerInwardDirections = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            _cornerBasePositions[i] = _corners[i].anchoredPosition;

            // 왼쪽 모서리는 오른쪽(+x), 아래 모서리는 위쪽(+y)이 안쪽
            Vector2 anchor = _corners[i].anchorMin;
            _cornerInwardDirections[i] = new Vector2(anchor.x < 0.5f ? 1f : -1f, anchor.y < 0.5f ? 1f : -1f);
        }
    }

    private void OnEnable()
    {
        _remaining = _hostileCount;
        _hitFlashTimer = 0f;
        _pinchTimer = 0f;
        _punchTimer = 0f;
        _clearTimer = 0f;
        RefreshHostileText(1f);
    }

    // 명중 1회 = 1마리 처치. 이번 명중으로 0이 되었으면 true.
    public bool RegisterHit()
    {
        _hitFlashTimer = _hitFlashDuration;
        _pinchTimer = _pinchDuration;

        // AREA CLEAR 표시 중에는 숫자를 줄이지 않는다.
        if (IsClearing)
        {
            return false;
        }

        _remaining = Mathf.Max(0, _remaining - 1);
        if (_remaining == 0)
        {
            _clearTimer = _clearDuration;
            if (_hostileText != null)
            {
                _hostileText.text = _clearText;
            }
            return true;
        }

        _punchTimer = _numberPunchDuration;
        RefreshHostileText(_numberPunchScale);
        return false;
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        _time += deltaTime;

        UpdateCorners(deltaTime);
        UpdateCornerColor(deltaTime);
        UpdateHostileText(deltaTime);
    }

    private void UpdateCorners(float deltaTime)
    {
        if (_corners == null)
        {
            return;
        }

        // 숨쉬기: 0 → depth → 0 (cos 곡선)
        float breath = _breathPeriod > 0f
            ? _breathDepth * (1f - Mathf.Cos(_time / _breathPeriod * Mathf.PI * 2f)) * 0.5f
            : 0f;

        // 명중: 순간 최대로 오므렸다가 ease-out으로 복귀
        float pinch = 0f;
        if (_pinchTimer > 0f)
        {
            _pinchTimer = Mathf.Max(0f, _pinchTimer - deltaTime);
            float remain = _pinchDuration > 0f ? _pinchTimer / _pinchDuration : 0f;
            pinch = _pinchDepth * remain * remain;
        }

        for (int i = 0; i < _corners.Length; i++)
        {
            if (_corners[i] != null)
            {
                _corners[i].anchoredPosition = _cornerBasePositions[i] + _cornerInwardDirections[i] * (breath + pinch);
            }
        }
    }

    private void UpdateCornerColor(float deltaTime)
    {
        if (_clearTimer > 0f)
        {
            _clearTimer -= deltaTime;
            if (_clearTimer <= 0f)
            {
                _remaining = _hostileCount;
                RefreshHostileText(1f);
            }
        }

        Color color = IsClearing ? _clearColor : _baseColor;
        if (_hitFlashTimer > 0f)
        {
            _hitFlashTimer = Mathf.Max(0f, _hitFlashTimer - deltaTime);
            float t = _hitFlashDuration > 0f ? _hitFlashTimer / _hitFlashDuration : 0f;
            color = Color.Lerp(color, _hitFlashColor, t);
        }

        if (_cornerGraphics == null)
        {
            return;
        }

        foreach (Graphic graphic in _cornerGraphics)
        {
            if (graphic != null)
            {
                graphic.color = color;
            }
        }
    }

    // 숫자만 커졌다 돌아오도록 TMP rich text의 <size=%> 태그를 쓴다.
    private void UpdateHostileText(float deltaTime)
    {
        if (_punchTimer <= 0f || IsClearing)
        {
            return;
        }

        _punchTimer = Mathf.Max(0f, _punchTimer - deltaTime);
        float t = _numberPunchDuration > 0f ? 1f - _punchTimer / _numberPunchDuration : 1f;
        float eased = 1f - (1f - t) * (1f - t);
        RefreshHostileText(Mathf.Lerp(_numberPunchScale, 1f, eased));
    }

    private void RefreshHostileText(float numberScale)
    {
        if (_hostileText == null)
        {
            return;
        }

        int percent = Mathf.RoundToInt(numberScale * 100f);
        _hostileText.text = percent == 100
            ? _hostileLabel + _remaining
            : $"{_hostileLabel}<size={percent}%>{_remaining}</size>";
    }
}
