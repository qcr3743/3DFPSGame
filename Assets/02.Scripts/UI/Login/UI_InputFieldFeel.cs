using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 입력칸 상태(기본 / hover / focus)에 따라 테두리와 바탕 색을 부드럽게 바꾼다.
// focus 판단은 TMP_InputField의 onSelect / onDeselect 이벤트로 한다.
public class UI_InputFieldFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("대상")]
    [SerializeField] private TMP_InputField _inputField;   // 비어 있으면 같은 오브젝트에서 찾는다
    [SerializeField] private Image _border;
    [SerializeField] private Image _background;

    [Header("테두리 색")]
    [SerializeField] private Color _borderNormal = new Color(1f, 1f, 1f, 0.18f);
    [SerializeField] private Color _borderHover = new Color(1f, 1f, 1f, 0.32f);
    [SerializeField] private Color _borderFocus = new Color(0.62f, 0.9f, 0.18f, 1f);      // Accent #9EE62E

    [Header("바탕 색")]
    [SerializeField] private Color _backgroundNormal = new Color(1f, 1f, 1f, 0.04f);
    [SerializeField] private Color _backgroundFocus = new Color(0.62f, 0.9f, 0.18f, 0.06f); // Accent alpha 0.06

    [Header("연출")]
    [SerializeField] private float _fadeDuration = 0.12f;

    [Header("효과음 (비어 있으면 무시)")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _focusClip;

    private bool _isHovered;
    private bool _isFocused;

    // 상태가 바뀐 순간의 색(from)에서 목표 색(to)까지 _fadeDuration 동안 보간한다.
    private Color _borderFrom;
    private Color _borderTo;
    private Color _backgroundFrom;
    private Color _backgroundTo;
    private float _elapsed;

    private void Awake()
    {
        if (_inputField == null)
        {
            _inputField = GetComponent<TMP_InputField>();
        }
    }

    private void OnEnable()
    {
        if (_inputField != null)
        {
            _inputField.onSelect.AddListener(HandleSelect);
            _inputField.onDeselect.AddListener(HandleDeselect);
        }

        // 켜질 때는 연출 없이 기본 색으로 맞춘다.
        _isHovered = false;
        _isFocused = false;
        SetColorsImmediate(_borderNormal, _backgroundNormal);
    }

    private void OnDisable()
    {
        if (_inputField != null)
        {
            _inputField.onSelect.RemoveListener(HandleSelect);
            _inputField.onDeselect.RemoveListener(HandleDeselect);
        }
    }

    private void Update()
    {
        if (_elapsed >= _fadeDuration)
        {
            return;
        }

        _elapsed += Time.unscaledDeltaTime;
        float t = _fadeDuration > 0f ? Mathf.Clamp01(_elapsed / _fadeDuration) : 1f;

        if (_border != null)
        {
            _border.color = Color.Lerp(_borderFrom, _borderTo, t);
        }

        if (_background != null)
        {
            _background.color = Color.Lerp(_backgroundFrom, _backgroundTo, t);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        RefreshTarget();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        RefreshTarget();
    }

    private void HandleSelect(string text)
    {
        _isFocused = true;
        if (_audioSource != null && _focusClip != null)
        {
            _audioSource.PlayOneShot(_focusClip);
        }
        RefreshTarget();
    }

    private void HandleDeselect(string text)
    {
        _isFocused = false;
        RefreshTarget();
    }

    // 우선순위: focus > hover > 기본
    private void RefreshTarget()
    {
        Color borderTarget = _isFocused ? _borderFocus : (_isHovered ? _borderHover : _borderNormal);
        Color backgroundTarget = _isFocused ? _backgroundFocus : _backgroundNormal;

        _borderFrom = _border != null ? _border.color : borderTarget;
        _backgroundFrom = _background != null ? _background.color : backgroundTarget;
        _borderTo = borderTarget;
        _backgroundTo = backgroundTarget;
        _elapsed = 0f;
    }

    private void SetColorsImmediate(Color border, Color background)
    {
        _borderFrom = _borderTo = border;
        _backgroundFrom = _backgroundTo = background;
        _elapsed = _fadeDuration;

        if (_border != null)
        {
            _border.color = border;
        }

        if (_background != null)
        {
            _background.color = background;
        }
    }
}
