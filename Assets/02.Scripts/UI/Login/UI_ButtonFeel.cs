using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 버튼의 손맛 연출. Hover: 살짝 밝아지고 위로 1px, Press: 아래로 1px + 어두워짐.
// 버튼 루트(클릭 영역)는 그대로 두고, 자식 Visual만 움직여서
// 1) 레이아웃 그룹이 위치를 덮어쓰지 않고 2) 버튼 가장자리에서 hover가 깜빡이지 않게 한다.
public class UI_ButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("대상")]
    [SerializeField] private RectTransform _moveTarget;    // 위아래로 움직일 대상 (비어 있으면 자기 자신)
    [SerializeField] private Graphic[] _graphics;          // 밝기를 바꿀 대상 (비어 있으면 자식 전체)

    [Header("연출")]
    [SerializeField] private float _fadeDuration = 0.15f;  // 기본 ↔ hover 전환 시간
    [SerializeField] private float _pressDuration = 0.06f; // 눌림 전환 시간 (누를 때는 빠르게 반응)
    [SerializeField] private float _hoverLighten = 0.15f;  // hover 때 흰색 쪽으로 섞는 비율
    [SerializeField] private float _pressDarken = 0.2f;    // press 때 어둡게 하는 비율
    [SerializeField] private float _hoverOffsetY = 1f;
    [SerializeField] private float _pressOffsetY = -1f;

    [Header("효과음 (비어 있으면 무시)")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _hoverClip;
    [SerializeField] private AudioClip _clickClip;

    private Selectable _selectable;
    private Color[] _baseColors;
    private Vector2 _basePosition;

    // -1 = 눌림, 0 = 기본, 1 = hover. 현재 값을 목표 값으로 서서히 옮기며 색/위치를 계산한다.
    private float _level;
    private float _targetLevel;
    private bool _isHovered;
    private bool _isPressed;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();

        if (_moveTarget == null)
        {
            _moveTarget = transform as RectTransform;
        }

        if (_graphics == null || _graphics.Length == 0)
        {
            _graphics = GetComponentsInChildren<Graphic>(true);
        }

        _baseColors = new Color[_graphics.Length];
        for (int i = 0; i < _graphics.Length; i++)
        {
            _baseColors[i] = _graphics[i] != null ? _graphics[i].color : Color.white;
        }

        _basePosition = _moveTarget.anchoredPosition;
    }

    private void OnDisable()
    {
        // 패널 전환 등으로 hover 중에 꺼져도 다음에 켜질 때 원래 모습으로 돌아오게 한다.
        _isHovered = false;
        _isPressed = false;
        _level = 0f;
        _targetLevel = 0f;
        Apply();
    }

    private void Update()
    {
        if (Mathf.Approximately(_level, _targetLevel))
        {
            return;
        }

        float duration = _isPressed ? _pressDuration : _fadeDuration;
        float speed = 1f / Mathf.Max(duration, 0.0001f);
        _level = Mathf.MoveTowards(_level, _targetLevel, speed * Time.unscaledDeltaTime);
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        if (IsInteractable())
        {
            PlayClip(_hoverClip);
        }
        RefreshTarget();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        RefreshTarget();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !IsInteractable())
        {
            return;
        }

        _isPressed = true;
        PlayClip(_clickClip);
        RefreshTarget();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        _isPressed = false;
        RefreshTarget();
    }

    private void RefreshTarget()
    {
        if (!IsInteractable())
        {
            _targetLevel = 0f;
            return;
        }

        _targetLevel = _isPressed ? -1f : (_isHovered ? 1f : 0f);
    }

    private bool IsInteractable()
    {
        return _selectable == null || _selectable.IsInteractable();
    }

    private void Apply()
    {
        if (_baseColors == null || _moveTarget == null)
        {
            return;
        }

        for (int i = 0; i < _graphics.Length; i++)
        {
            if (_graphics[i] == null)
            {
                continue;
            }

            Color baseColor = _baseColors[i];
            Color color = _level >= 0f
                ? Color.Lerp(baseColor, Lighten(baseColor), _level)
                : Color.Lerp(baseColor, Darken(baseColor), -_level);
            _graphics[i].color = color;
        }

        float offsetY = _level >= 0f ? _hoverOffsetY * _level : _pressOffsetY * -_level;
        _moveTarget.anchoredPosition = _basePosition + new Vector2(0f, offsetY);
    }

    // 흰색 쪽으로 살짝 섞는다. 테두리처럼 원래 흰색이고 alpha가 낮은 경우도 밝아지도록 alpha도 같이 올린다.
    private Color Lighten(Color color)
    {
        Color result = Color.Lerp(color, Color.white, _hoverLighten);
        result.a = Mathf.Lerp(color.a, 1f, _hoverLighten);
        return result;
    }

    private Color Darken(Color color)
    {
        float factor = 1f - _pressDarken;
        return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
    }

    private void PlayClip(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }
}
