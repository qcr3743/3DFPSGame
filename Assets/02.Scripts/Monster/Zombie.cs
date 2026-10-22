using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;

// FSM(유한 상태 머신): 유한개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - 상태: 객체가 가질 수 있는 어떤 조건이나 상황
// - 전이: 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"

// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle, // 대기: 제자리에서 플레이어를 기다림
    Trace, // 추격: 플레이어를 쫓아감
    Comeback, // 복귀: 처음 자리로 돌아감
    Attack, // 공격: 일정 시간마다 플레이어를 때림
    Hit, // 피격: 공격(총알, 폭탄)을 받아 잠깐 멈칫함
    Death // 죽음: 쓰러지고 잠시 뒤 사라짐
}

public class Zombie : MonoBehaviour
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8f;

    [SerializeField] private float _combackDistance = 15f; // 처음 자리에서 멀어지면 돌아가는 거리
    [SerializeField] private float _moveSpeed = 5f;

    private CharacterController _controller;
    private Vector3 _startPosition;
    private GameObject _player;
    private ZombieState _state = ZombieState.Idle;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _startPosition = transform.position;
        _player = GameObject.FindWithTag("Player");
    }

    private void Update()
    {
        switch (_state)
        {
            case ZombieState.Idle:
                Idle();
                break;
            case ZombieState.Trace:
                Trace();
                break;
            case ZombieState.Comeback:
                Comeback();
                break;
            case ZombieState.Attack:
                Attack();
                break;
            case ZombieState.Hit:
                break;
            case ZombieState.Death:
                break;
        }
    }

    private void Idle()
    {
        // Todo. Idle 애니메이션 실행

        // 플레이어가 가까워지면 Trace 상태로 전이됨
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance <= _detectDistance)
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
        }
    }

    // 추격 플레이어를 쫓아감
    private void Trace()
    {
        // Todo. Run 애니메이션 실행

        if (Vector3.Distance(transform.position, _startPosition) > _combackDistance)
        {
            Debug.Log($"{_state} -> Comeback");
            _state = ZombieState.Comeback;
            return;
        }

        Vector3 direction = _player.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    private void Comeback()
    {
        if (Vector3.Distance(transform.position, _startPosition) < 1f)
        {
            Debug.Log($"{_state} -> Idle");
            _state = ZombieState.Idle;
            return;
        }

        Vector3 direction = _startPosition - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    private void Attack()
    {
    }
}