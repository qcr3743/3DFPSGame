using System.Collections;
using UnityEngine;

// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [SerializeField] private int _maxBulletCount = 50;
    [SerializeField] private float _reloadingTime = 3f;
    [SerializeField] private float _range = 100f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _fireInterval = 0.1f;

    private float _fireTimer = 0f;
    private int _bulletCount = 0;
    private Coroutine _reloadCoroutine;

    private void Start()
    {
        _bulletCount = _maxBulletCount;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        // 0. 발사 타이머를 지난 시간만큼 줄인다
        _fireTimer -= Time.deltaTime;

        bool isMouseVisible = Cursor.visible;
        // 1. 마우스 왼쪽 버튼 입력 받기
        if (!isMouseVisible && Input.GetButton("Fire1") && _fireTimer <= 0f && _bulletCount > 0)
        {
            _fireTimer = _fireInterval;
            Shoot();

            if (_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
                Debug.Log("재장전 취소!");
                _reloadCoroutine = null;
            }
        }

        if (Input.GetKeyDown(KeyCode.R) && _reloadCoroutine == null)
        {
            _reloadCoroutine = StartCoroutine(Reloading_Coroutine());
        }
    }

    private IEnumerator Reloading_Coroutine()
    {
        Debug.Log("재장전 시작!");

        yield return new WaitForSeconds(_reloadingTime);
        _bulletCount = _maxBulletCount;

        Debug.Log("재장전 완료!");
    }

    private void Shoot()
    {
        // Ray: 레이저(시작위치, 날아갈 방향)
        // Physics.RayCast: 레이저 발사
        // RayCastHit: 레이저가 물체와 부딪히면 그 정보를 저장하는 구조체

        // 2. 레이(광선)을 생성하고 발사될 위치와 진행 방향 설정하기
        Transform cameraTransform = Camera.main.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딪힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딪힌 물체가 있다면 이펙트 표시하기
        bool isHit = Physics.Raycast(ray, out hitInfo, _range);
        if (isHit)
        {
            // Debug.Log($"{hitInfo.collider.name}이 총알을 맞았다. 데미지: {_damage}");
            ParticleSystem effect = Instantiate(_hitEffectPrefab);
            effect.transform.position = hitInfo.point; // 맞은 자리
            effect.transform.forward = hitInfo.normal; // 수직 위치로 회전
            effect.Play();

            // 데미지 구조체 만들기
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;

            if (hitInfo.collider.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damageInfo);
            }

            /*
            if (hitInfo.collider.TryGetComponent(out Zombie zombie))
            {
                zombie.TakeDamage(_damage);
            }
            if (hitInfo.collider.TryGetComponent(out Drum drum))
            {
                drum.TakeDamage(_damage);
            }
            hitInfo.collider.GetComponent<Box>()?.TakeDamage(_damage);
            */
        }

        _bulletCount--;
    }
}