using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _staminaSlider;

    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private Player _player;

    private void LateUpdate()
    {
        _staminaSlider.value = _playerMove.Stamina / PlayerMove.MaxStamina;
        _healthSlider.value = _player.Health / _player.MaxHealth;
    }
}