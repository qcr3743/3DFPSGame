using UnityEngine;

// 서치라이트 빛줄기를 피벗(빛줄기 아래쪽 끝) 기준으로 좌우로 천천히 왕복 회전시킨다.
public class UI_SearchlightSweep : MonoBehaviour
{
    [SerializeField] private RectTransform _target;   // 비어 있으면 자기 자신 (pivot이 빛줄기 아래 끝이어야 함)
    [SerializeField] private float _minAngle = -25f;  // 음수 = 오른쪽으로 기울어짐 (시계 방향)
    [SerializeField] private float _maxAngle = 5f;
    [SerializeField] private float _period = 10f;     // 한 번 왕복하는 데 걸리는 시간(초)

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
        if (_target == null || _period <= 0f)
        {
            return;
        }

        _time += Time.unscaledDeltaTime;

        // cos 곡선: 끝에서 느려졌다가 다시 돌아오는 자연스러운 왕복 (0초에 _minAngle에서 시작)
        float t = (1f - Mathf.Cos(_time / _period * Mathf.PI * 2f)) * 0.5f;
        float angle = Mathf.Lerp(_minAngle, _maxAngle, t);
        _target.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
