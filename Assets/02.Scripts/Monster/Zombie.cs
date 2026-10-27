using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8f;

    [SerializeField] private float _combackDistance = 15f; // 처음 자리에서 멀어지면 돌아가는 거리
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackInterval = 1.2f;
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _maxHealth = 30f;
    [SerializeField] private float _health = 30f;

    [SerializeField] private GameObject _bloodSprayPrefab;

    private float _attackTimer = 0f;
    private Vector3 _startPosition;
    private GameObject _player;
    private NavMeshAgent _agent;
    private Animator _animator;

    public event Action OnDamaged;
    public event Action OnDied;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    private ZombieState _state = ZombieState.Idle;

    private void Start()
    {
        _health = _maxHealth;

        // 길을 찾아 걷게 해주는 컴포넌트 NavMeshAgent 참조
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _moveSpeed;
        _startPosition = transform.position;
        _player = GameObject.FindWithTag("Player");
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

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
        }
    }

    // 데미지를 받음
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }


        _health -= damageInfo.Value;

        OnDamaged?.Invoke();

        StopAllCoroutines();

        if (_health > 0f)
        {
            // 좀비의 자식으로 블러드 스프레이를 생성! -> 위치 수정!
            GameObject bloodSpray = Instantiate(_bloodSprayPrefab, transform);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;
            // 잠깐 멈칫
            Debug.Log($"{_state} -> Hit");
            _state = ZombieState.Hit;
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            Debug.Log($"{_state} -> Death");
            _animator.SetTrigger("Death");
            _state = ZombieState.Death;
            StartCoroutine(Death_Coroutine());
            OnDied?.Invoke();
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        StopAgent();
        yield return new WaitForSeconds(0.3f); // 잠깐 멈칫함

        Debug.Log($"{_state} -> Trace");
        _state = ZombieState.Trace;
    }

    private IEnumerator Death_Coroutine()
    {
        StopAgent();

        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    private void Idle()
    {
        StopAgent();

        // 플레이어가 가까워지면 Trace 상태로 전이됨
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance <= _detectDistance)
        {
            Debug.Log($"{_state} -> Trace");
            _animator.SetTrigger("IdleToMove");
            _state = ZombieState.Trace;
        }
    }

    // 추격 플레이어를 쫓아감
    private void Trace()
    {
        // Move 애니메이션은 Idle에서 IdleToMove 트리거로 이미 실행 중

        if (Vector3.Distance(transform.position, _startPosition) > _combackDistance)
        {
            Debug.Log($"{_state} -> Comeback");
            _state = ZombieState.Comeback;
            return;
        }

        // 플레이어가 공격 거리 안이면 공격
        if (Vector3.Distance(transform.position, _player.transform.position) <= _attackRange)
        {
            _attackTimer = 0f;
            Debug.Log($"{_state} -> Attack");
            _animator.SetTrigger("MoveToAttackDelay");
            _state = ZombieState.Attack;
            return;
        }

        // SetDestination은 목적지 설정
        // - 호출할 때마다 목적리를 향해 조금씩 이동 (X)
        // - 한 번 세팅하면 내부에서 계속해서 이동 (O)
        _agent.SetDestination(_player.transform.position);
    }

    private void Comeback()
    {
        if (Vector3.Distance(transform.position, _startPosition) < _agent.stoppingDistance)
        {
            Debug.Log($"{_state} -> Idle");
            _animator.SetTrigger("MoveToIdle");
            _state = ZombieState.Idle;
            return;
        }

        _agent.SetDestination(_startPosition);
    }

    private void Attack()
    {
        StopAgent();

        // 플레이어가 공격 사정 거리 밖으로 나가면 추격 개시
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            Debug.Log($"{_state} -> Trace");
            _animator.SetTrigger("AttackMove");
            _state = ZombieState.Trace;
            return;
        }

        _attackTimer += Time.deltaTime;

        if (_attackTimer >= _attackInterval)
        {
            _animator.SetTrigger("StartAttack");

            Debug.Log($"플레이어 공격");
            _attackTimer = 0f;
        }
    }

    public void AttackAction()
    {
        DamageInfo damageInfo = new DamageInfo()
        {
            Value = _attackDamage,
            HitPoint = _player.transform.position,
            HitNormal = (transform.position - _player.transform.forward).normalized
        };

        _player.GetComponent<IDamageable>().TakeDamage(damageInfo);
    }

    private void StopAgent()
    {
        _agent.ResetPath(); // 에이전트의 목적지 삭제
        _agent.velocity = Vector3.zero; // 미끄럼 방지를 위해 가속도 제로!
    }
}