using System.Collections;
using UnityEngine;

// 마우스 오른쪽 버튼을 누르면 시선이 바라보는 방향으로 수류탄을 던짐
public class PlayerBombFire : MonoBehaviour
{
    [SerializeField] private BombPool _bombPool;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private int _bombCount = 10;

    [SerializeField] private float _minPower = 8f;
    [SerializeField] private float _maxPower = 30f;
    [SerializeField] private float _maxChargeTime = 2f;

    [Header("던지기 애니메이션 타이밍")]
    [Tooltip("Throw 애니메이션 시작 후 손에서 폭탄이 떠나는 시간(초). 놓는 프레임 / 30")]
    [SerializeField] private float _releaseDelay = 0.8f;
    [Tooltip("한 번 던진 뒤 다음 던지기까지 막는 시간(초). Throw 클립 길이 정도")]
    [SerializeField] private float _throwDuration = 1.9f;

    private float _throwPower;
    private float _chargeTime;
    private bool _isCharging;   // GetButtonDown을 정상적으로 받은 뒤에만 true
    private bool _isThrowing;   // 던지는 애니메이션이 진행 중이면 true

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        // 폭탄이 없거나 이미 던지는 중이면 새 입력을 받지 않는다
        if (_bombCount <= 0 || _isThrowing)
        {
            return;
        }

        // 1. 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButtonDown("Fire2"))
        {
            _chargeTime = 0f;
            _isCharging = true;
        }

        if (_isCharging && Input.GetButton("Fire2"))
        {
            _chargeTime += Time.deltaTime;
            _chargeTime = Mathf.Clamp(_chargeTime, 0f, _maxChargeTime);
        }

        // 던지는 중에 눌렀다가 끝난 뒤에 뗀 경우는 _isCharging이 false라 무시된다
        if (_isCharging && Input.GetButtonUp("Fire2"))
        {
            _isCharging = false;

            float chargeRatio = _chargeTime / _maxChargeTime;
            _throwPower = Mathf.Lerp(_minPower, _maxPower, chargeRatio);

            StartCoroutine(Throw_Coroutine());
        }
    }

    private IEnumerator Throw_Coroutine()
    {
        _isThrowing = true;

        // 2. 애니메이션 먼저 재생
        _animator.SetTrigger("Throw");

        // 3. 손에서 폭탄이 떠나는 시점까지 기다렸다가 발사
        yield return new WaitForSeconds(_releaseDelay);
        if (GameManager.Instance.State == GameState.Playing)
        {
            Shoot();
        }

        // 4. 남은 애니메이션이 끝날 때까지 다음 던지기를 막는다
        yield return new WaitForSeconds(Mathf.Max(0f, _throwDuration - _releaseDelay));
        _isThrowing = false;
    }

    private void Shoot()
    {
        Bomb bomb = _bombPool.Get(_firePoint.position);

        Rigidbody rigidbody = bomb.GetComponent<Rigidbody>();
        rigidbody.AddForce(Camera.main.transform.forward * _throwPower, ForceMode.Impulse);
        _bombCount--;
    }
}
