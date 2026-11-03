using UnityEngine;
using UnityEngine.UI;

// LIVE 점, SERVER 점처럼 alpha를 1 ↔ 0.3 사이로 계속 깜빡이게 한다.
// 일시정지(Time.timeScale)와 무관하게 동작하도록 unscaledDeltaTime을 쓴다.
public class UI_Blink : MonoBehaviour
{
    [SerializeField] private Graphic _target;          // 비어 있으면 같은 오브젝트의 Graphic(Image, TMP 등)
    [SerializeField] private float _period = 1.2f;     // 한 번 어두워졌다 밝아지는 데 걸리는 시간(초)
    [SerializeField] private float _maxAlpha = 1f;
    [SerializeField] private float _minAlpha = 0.3f;

    private Color _baseColor;
    private float _time;

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
    }

    private void Update()
    {
        if (_target == null || _period <= 0f)
        {
            return;
        }

        _time += Time.unscaledDeltaTime;

        // cos 곡선: 0초에 1(가장 밝음)에서 시작해 반 주기에 0(가장 어두움)이 된다.
        float wave = (Mathf.Cos(_time / _period * Mathf.PI * 2f) + 1f) * 0.5f;

        Color color = _baseColor;
        color.a = _baseColor.a * Mathf.Lerp(_minAlpha, _maxAlpha, wave);
        _target.color = color;
    }
}
