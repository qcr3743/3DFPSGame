using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    private Zombie _zombie;

    private void Awake()
    {
        _zombie = GetComponentInParent<Zombie>();
    }

    private void Start()
    {
        if (_zombie != null)
        {
            _zombie.OnDamaged += OnZombieDamaged;
        }
    }

    private void OnDestroy()
    {
        if (_zombie != null)
        {
            _zombie.OnDamaged -= OnZombieDamaged;
        }
    }

    private void OnZombieDamaged()
    {
        _slider.value = _zombie.Health / _zombie.MaxHealth;
    }

    private void LateUpdate()
    {
        //빌보드가 체력바가 카메라를 쳐다보게한다
        _slider.transform.forward = Camera.main.transform.forward;
    }
}