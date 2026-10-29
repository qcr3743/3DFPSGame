using System;
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

    [SerializeField] private GameObject[] _muzzleFlashes;
    [SerializeField] private float _flashTime = 0.05f;

    // 총알 궤적: 총구에서 맞은 지점까지 잠깐 선을 그린다
    [SerializeField] private Transform _firePoint;        // 선이 시작되는 총구 위치
    [SerializeField] private LineRenderer _bulletTrail;   // 궤적을 그릴 LineRenderer
    [SerializeField] private float _trailTime = 0.03f;    // 선이 보이는 시간

    private float _fireTimer = 0f;
    private int _bulletCount = 0;
    private Coroutine _reloadCoroutine;
    private Coroutine _trailCoroutine; // 실행 중인 궤적 코루틴 (연사 시 이전 것을 멈추기 위해 저장)

    private Animator _animator;

    public event Action<int, int> OnBulletCountChanged;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        _bulletCount = _maxBulletCount;
        OnBulletCountChanged?.Invoke(_bulletCount, _maxBulletCount);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        // 0. 발사 타이머를 지난 시간만큼 줄인다
        _fireTimer -= Time.deltaTime;

        bool canFire = !Cursor.visible && Input.GetButton("Fire1") && _bulletCount > 0;
        _animator.SetBool("Fire", canFire);
        // 1. 마우스 왼쪽 버튼 입력 받기

        if (canFire && _fireTimer <= 0f)
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
        OnBulletCountChanged?.Invoke(_bulletCount, _maxBulletCount);

        Debug.Log("재장전 완료!");
        _reloadCoroutine = null;
    }

    private void Shoot()
    {
        StartCoroutine(MuzzleFlash_Coroutine());
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

        // 총알 궤적 표시: 맞았으면 맞은 곳까지, 안 맞았으면 사거리 끝까지
        Vector3 endPoint = isHit ? hitInfo.point : ray.GetPoint(_range);
        ShowBulletTrail(endPoint);

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
        OnBulletCountChanged?.Invoke(_bulletCount, _maxBulletCount);
    }

    private IEnumerator MuzzleFlash_Coroutine()
    {
        int randomIndex = UnityEngine.Random.Range(0, _muzzleFlashes.Length);
        GameObject flash = _muzzleFlashes[randomIndex];
        flash.SetActive(true);

        yield return new WaitForSeconds(_flashTime);

        flash.SetActive(false);
    }

    // 총알 궤적 표시 시작
    private void ShowBulletTrail(Vector3 endPoint)
    {
        // 연사 중에 이전 코루틴이 새로 그린 선을 꺼 버리지 않도록 멈춘다
        if (_trailCoroutine != null)
        {
            StopCoroutine(_trailCoroutine);
        }
        _trailCoroutine = StartCoroutine(BulletTrail_Coroutine(endPoint));
    }

    // 총구 → 끝점으로 선을 그리고, 잠시 뒤 끈다
    private IEnumerator BulletTrail_Coroutine(Vector3 endPoint)
    {
        _bulletTrail.SetPosition(0, _firePoint.position); // 시작점: 총구
        _bulletTrail.SetPosition(1, endPoint);            // 끝점: 맞은 곳
        _bulletTrail.enabled = true;

        yield return new WaitForSeconds(_trailTime);

        _bulletTrail.enabled = false;
        _trailCoroutine = null;
    }
}