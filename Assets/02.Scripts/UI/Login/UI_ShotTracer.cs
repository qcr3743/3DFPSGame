using UnityEngine;
using UnityEngine.UI;

// 총구 → 클릭 지점 탄도선. 총구 쪽 끝에서부터 줄어들어 명중 지점 쪽으로 사라진다.
// 선의 피벗을 명중 지점 쪽 끝(pivot x = 1)에 두고, 길이(sizeDelta.x)만 줄인다.
public class UI_ShotTracer : MonoBehaviour
{
    [SerializeField] private Graphic _line;              // 비어 있으면 같은 오브젝트의 Graphic
    [SerializeField] private float _duration = 0.06f;
    [SerializeField] private float _thickness = 2f;
    [SerializeField] private float _alpha = 0.9f;

    private RectTransform _rect;
    private Vector2 _from;
    private Vector2 _to;
    private float _elapsed;
    private float _freezeTimer;
    private bool _isActive;

    private void Awake()
    {
        if (_line == null)
        {
            _line = GetComponent<Graphic>();
        }

        _rect = _line.rectTransform;
        _rect.pivot = new Vector2(1f, 0.5f);
    }

    private void OnEnable()
    {
        _isActive = false;
        SetAlpha(0f);
    }

    public void Play(Vector2 from, Vector2 to)
    {
        _from = from;
        _to = to;
        _elapsed = 0f;
        _isActive = true;
        Apply(0f);
    }

    // 히트스톱: 잠깐 그 상태로 멈춘다.
    public void Freeze(float duration)
    {
        _freezeTimer = Mathf.Max(_freezeTimer, duration);
    }

    private void Update()
    {
        if (!_isActive)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        if (_freezeTimer > 0f)
        {
            _freezeTimer -= deltaTime;
            return;
        }

        _elapsed += deltaTime;
        float t = _duration > 0f ? _elapsed / _duration : 1f;
        if (t >= 1f)
        {
            _isActive = false;
            SetAlpha(0f);
            return;
        }

        Apply(t);
    }

    private void Apply(float t)
    {
        Vector2 direction = _to - _from;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        _rect.anchoredPosition = _to;
        _rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        _rect.sizeDelta = new Vector2(direction.magnitude * (1f - t), _thickness);
        SetAlpha(_alpha);
    }

    private void SetAlpha(float alpha)
    {
        if (_line == null)
        {
            return;
        }

        Color color = _line.color;
        color.a = alpha;
        _line.color = color;
    }
}
