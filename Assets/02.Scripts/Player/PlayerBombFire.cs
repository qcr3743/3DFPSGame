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
    private float _throwPower;
    private float _chargeTime;

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (_bombCount <= 0)
        {
            return;
        }

        // 1. 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButtonDown("Fire2"))
        {
            _chargeTime = 0f;
        }

        if (Input.GetButton("Fire2"))
        {
            _chargeTime += Time.deltaTime;
            _chargeTime = Mathf.Clamp(_chargeTime, 0f, _maxChargeTime);
        }

        if (Input.GetButtonUp("Fire2"))
        {
            float chargeRatio = _chargeTime / _maxChargeTime;
            _throwPower = Mathf.Lerp(_minPower, _maxPower, chargeRatio);
            Shoot();
        }
    }

    private void Shoot()
    {
        Bomb bomb = _bombPool.Get(_firePoint.position);

        Rigidbody rigidbody = bomb.GetComponent<Rigidbody>();
        rigidbody.AddForce(Camera.main.transform.forward * _throwPower, ForceMode.Impulse);
        _bombCount--;
    }
}