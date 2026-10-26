using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _staminaSlider;

    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private Player _player;

    private void Start()
    {
        // 플레이어 공격 이벤트 구독
        _player.OnDamaged += RefreshHealth;
    }

    private void LateUpdate()
    {
        RefreshHealth();

        _staminaSlider.value = _playerMove.Stamina / PlayerMove.MaxStamina;
    }

    private void RefreshHealth()
    {
        // 문제: 체력값이 변하지 않았음에도 초당 60회 이상 캔버스를 불필요하게 다시 그려서 성능 낭비 발생
        // 이러한 연산이 적립될 수록 UI가 느려짐
        // 해결: 값이 바뀔때만 캔버스 갱신

        // 옵저버 패턴 사용으로 해결
        _healthSlider.value = _player.Health / _player.MaxHealth;
    }
}