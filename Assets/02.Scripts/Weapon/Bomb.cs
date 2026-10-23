using UnityEngine;

// 무언가에 부딪히면 폭발 이펙트를 남기고 사라지는 폭탄
public class Bomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField] private float _damage = 30f;
    [SerializeField] private float _explosionRange = 4.5f;

    private BombPool _pool;
    private Rigidbody _rigidbody;
    private bool _isExploded;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // 풀이 만들 때 한 번 불러줌
    public void Init(BombPool pool)
    {
        _pool = pool;
    }

    // 켜질 때마다 초기화
    private void OnEnable()
    {
        _isExploded = false;
        _rigidbody.linearVelocity = Vector3.zero; // Unity 6 이전: velocity
        _rigidbody.angularVelocity = Vector3.zero;
    }

    // 다른 콜라이더와 부딪힌 순간 실행되는 메서드
    private void OnCollisionEnter(Collision collision)
    {
        if (_isExploded)
        {
            return; // 같은 프레임 중복 충돌 방지
        }

        _isExploded = true;

        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        ApplyAreaDamage();

        _pool.Return(this); // Destroy 대신 풀로 복귀
    }

    private void ApplyAreaDamage()
    {
        // 0. 주위에 있는 드럼통을 모두 찾음
        // Physics.OverlapSphere(원점, 반지름, 찾을 레이어들)
        Collider[] colliders =
            Physics.OverlapSphere(transform.position, _explosionRange, LayerMask.GetMask("Drum", "Zombie"));

        // 1. 드럼통들에게 데미지를 줌
        foreach (Collider col in colliders)
        {
            DamageInfo damageInfo = new()
            {
                Value = _damage,
                HitPoint = col.ClosestPoint(transform.position),
                HitNormal = (transform.position - col.ClosestPoint(transform.position)).normalized
            };
            col.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }
    }
}