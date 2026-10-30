using UnityEngine;

// 근접 모드에서 마우스 오른쪽 버튼을 누르면 카타나를 휘두른다.
// 애니메이션 이벤트를 받아야 하므로 Animator가 붙은 Model에 붙임
public class PlayerMeleeAttack : MonoBehaviour
{
    [SerializeField] private MeleeHitMode _hitMode = MeleeHitMode.Collider;  // 맞힐 대상을 고르는 방법
    [SerializeField] private float _fanDamage = 25f;                         // 부채꼴 방법의 대미지
    [SerializeField] private float _range = 2f;                              // 부채꼴의 반지름 (m)
    [SerializeField, Range(0f, 360f)] private float _angle = 90f;            // 부채꼴 각도 (도)
    [SerializeField] private LayerMask _targetMask;                          // 때릴 대상 (Drum, Zombie)
    [SerializeField] private LayerMask _obstacleMask;                        // 가로막는 것 (Default)
    
    [SerializeField] private GameObject _katana;
    [SerializeField] private Collider _katanaCollider;
    
    private Player _player;
    private Animator _animator;

    private void Awake()
    {
        _player = GetComponentInParent<Player>();        // 부모의 Player
        _animator = GetComponentInChildren<Animator>();  // Goku 자신의 Animator
    }
    
    private void OnEnable()
    {
        // 구독: 무기 모드가 바뀌면 불러 달라고 등록한다.
        _player.OnWeaponModeChanged += RefreshKatana;
    }

    private void OnDisable()
    {
        // 구독 취소
        _player.OnWeaponModeChanged -= RefreshKatana;
    }
    
    private void Start()
    {
        RefreshKatana(_player.WeaponMode);  // 처음 모드에 맞춰 카타나를 보이거나 숨긴다.
    }
    
    private void RefreshKatana(WeaponMode mode)
    {
        _katana.SetActive(mode == WeaponMode.Melee);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }
        if (_player.WeaponMode != WeaponMode.Melee)
        {
            return; // 근접 모드에서만 휘두른다.
        }

        if (Input.GetButtonDown("Fire2"))
        {
            _animator.SetTrigger("Slash");
        }
    }
    
    // 앞쪽 부채꼴 안의 대상을 한 번에 때린다. slash8의 FanAttack 이벤트가 불러 준다.
    public void FanAttack()
    {
        // 칼날 콜라이더 방법일 때는 하지 않는다.
        if (_hitMode != MeleeHitMode.Fan)
        {
            return;
        }

        // Goku의 발밑이 아니라 Player의 가운데에서, Player의 앞쪽으로 잰다.
        Transform player = _player.transform;
        Vector3 origin = player.position;

        // 1. 사거리 안의 대상을 모두 찾는다.
        Collider[] colliders = Physics.OverlapSphere(origin, _range, _targetMask);
        foreach (Collider col in colliders)
        {
            // 2. 대상에서 플레이어에 가장 가까운 점을 맞을 자리로 삼는다.
            Vector3 hitPoint = col.ClosestPoint(origin);

            // 3. 높이를 뺀 방향이 앞쪽에서 Angle의 절반 안에 있는지 본다.
            Vector3 direction = hitPoint - origin;
            direction.y = 0f;
            if (Vector3.Angle(player.forward, direction) > _angle / 2f)
            {
                continue;
            }

            // 4. 가는 길에 벽이 있으면 때리지 않는다.
            if (Physics.Linecast(origin, hitPoint, _obstacleMask))
            {
                continue;
            }

            // 5. 대미지를 받을 수 있으면 대미지를 준다.
            if (col.TryGetComponent(out IDamageable damageable))
            {
                DamageInfo damageInfo = new DamageInfo();
                damageInfo.Value = _fanDamage;
                damageInfo.HitPoint = hitPoint;
                damageInfo.HitNormal = (origin - hitPoint).normalized;
                damageable.TakeDamage(damageInfo);
            }
        }
    }
    
    // 칼날을 켠다. slash8의 ActivateCollider 이벤트가 불러 준다.
    public void ActivateCollider()
    {
        // 부채꼴 방법일 때는 칼날을 켜지 않는다.
        if (_hitMode != MeleeHitMode.Collider)
        {
            return;
        }
        
        Debug.Log("카타나 콜라이더 활성화");
        _katanaCollider.enabled = true;
    }

    // 칼날을 끈다. slash8의 DeactivateCollider 이벤트가 불러 준다.
    public void DeactivateCollider()
    {
        Debug.Log("카타나 콜라이더 비활성화");
        _katanaCollider.enabled = false;
    }
    
    // Goku를 고르면 Scene 창에 부채꼴 방법의 공격 범위를 그린다.
    private void OnDrawGizmosSelected()
    {
        // 플레이하지 않을 때도 불리므로, Awake에서 찾는 _player 대신 여기서 찾는다.
        Player player = GetComponentInParent<Player>();
        if (player == null)
        {
            return;
        }

        Vector3 origin = player.transform.position;
        Vector3 forward = player.transform.forward;
        Gizmos.color = Color.yellow;

        // 앞쪽을 y축으로 -Angle/2, +Angle/2만큼 돌린 두 방향이 부채꼴의 양 끝이다.
        Vector3 left = Quaternion.Euler(0f, -_angle / 2f, 0f) * forward;
        Vector3 right = Quaternion.Euler(0f, _angle / 2f, 0f) * forward;
        Gizmos.DrawLine(origin, origin + left * _range);
        Gizmos.DrawLine(origin, origin + right * _range);

        // 호: 10도마다 짧은 선을 이어 그린다.
        int count = Mathf.Max(1, Mathf.CeilToInt(_angle / 10f));
        Vector3 previous = origin + left * _range;
        for (int i = 1; i <= count; i++)
        {
            float angle = -_angle / 2f + _angle * i / count;
            Vector3 next = origin + Quaternion.Euler(0f, angle, 0f) * forward * _range;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
