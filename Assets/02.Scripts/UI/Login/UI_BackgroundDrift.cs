using UnityEngine;

// 배경 이미지를 아주 천천히 확대/축소(1.0 ↔ 1.05)해서 정지 화면이 살아 있는 느낌을 준다.
public class UI_BackgroundDrift : MonoBehaviour
{
    [SerializeField] private RectTransform _target;    // 비어 있으면 자기 자신
    [SerializeField] private float _duration = 30f;    // 1.0 → 1.05 한 방향에 걸리는 시간(초)
    [SerializeField] private float _startScale = 1f;
    [SerializeField] private float _endScale = 1.05f;

    private float _time;

    private void Awake()
    {
        if (_target == null)
        {
            _target = transform as RectTransform;
        }
    }

    private void Update()
    {
        if (_target == null || _duration <= 0f)
        {
            return;
        }

        _time += Time.unscaledDeltaTime;

        // PingPong으로 0→1→0 왕복, SmoothStep으로 끝부분에서 부드럽게 감속한다.
        float t = Mathf.PingPong(_time / _duration, 1f);
        float scale = Mathf.SmoothStep(_startScale, _endScale, t);
        _target.localScale = new Vector3(scale, scale, 1f);
    }
}
