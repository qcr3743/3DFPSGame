using UnityEngine;
using UnityEngine.UI;

// 조명 glow의 alpha를 Perlin 노이즈로 일렁이게 하고, 설정한 확률로 가끔 짧게 꺼지게 한다.
// (연막탄 glow: 빠르게 일렁임 / 가로등 glow: 느리게 일렁이다 가끔 0.1초 꺼짐)
public class UI_LightFlicker : MonoBehaviour
{
    [SerializeField] private Graphic _target;              // 비어 있으면 같은 오브젝트의 Graphic

    [Header("일렁임")]
    [SerializeField] private float _minAlpha = 0.4f;
    [SerializeField] private float _maxAlpha = 0.55f;
    [SerializeField] private float _noiseSpeed = 1.5f;     // 클수록 빠르게 일렁인다

    [Header("꺼짐")]
    [SerializeField] private float _outChancePerSecond = 0f;  // 1초당 꺼질 확률 (0이면 꺼지지 않음)
    [SerializeField] private float _outDuration = 0.1f;
    [SerializeField] private float _outAlpha = 0f;

    private Color _baseColor;
    private float _noiseSeed;
    private float _time;
    private float _outTimer;

    private void Awake()
    {
        if (_target == null)
        {
            _target = GetComponent<Graphic>();
        }

        if (_target != null)
        {
            _baseColor = _target.color;
        }

        // 같은 스크립트를 붙인 조명끼리 같은 모양으로 일렁이지 않게 노이즈 시작점을 다르게 한다.
        _noiseSeed = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        if (_target == null)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        _time += deltaTime;

        // 꺼짐: 꺼져 있는 동안은 타이머만 줄이고, 켜져 있을 때 프레임마다 확률로 꺼짐을 시작한다.
        if (_outTimer > 0f)
        {
            _outTimer -= deltaTime;
        }
        else if (_outChancePerSecond > 0f && Random.value < _outChancePerSecond * deltaTime)
        {
            _outTimer = _outDuration;
        }

        float alpha;
        if (_outTimer > 0f)
        {
            alpha = _outAlpha;
        }
        else
        {
            float noise = Mathf.PerlinNoise(_noiseSeed, _time * _noiseSpeed);
            alpha = Mathf.Lerp(_minAlpha, _maxAlpha, noise);
        }

        Color color = _baseColor;
        color.a = alpha;
        _target.color = color;
    }
}
