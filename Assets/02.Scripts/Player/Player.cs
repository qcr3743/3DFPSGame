using System;
using TMPro;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private TextMeshProUGUI _modeText;

    private float _health;

    private Animator _animator;
    private int _injuredLayerIndex;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    private WeaponMode _weaponMode = WeaponMode.Normal;
    public WeaponMode WeaponMode => _weaponMode;

    // Action 자료형: 함수(들)을 저장할 수 있다.
    public event Action OnDamaged;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _injuredLayerIndex = _animator.GetLayerIndex("Injured Layer");

        _health = _maxHealth;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _weaponMode = WeaponMode.Normal;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _weaponMode = WeaponMode.Sniper;
        }
    }

    private void ChangeMode(WeaponMode mode)
    {
        _weaponMode = mode;
        ShowMode();
        Debug.Log($"무기 모드: {_weaponMode}");
    }

    private void ShowMode()
    {
        switch (_weaponMode)
        {
            case WeaponMode.Normal:
                _modeText.text = "일반 모드";
                break;
            case WeaponMode.Sniper:
                _modeText.text = "저격 모드";
                break;
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f)
        {
            return;
        }

        _health -= damageInfo.Value;

        // 체력에 따라 InJured Layer의 가중치를 바꿈
        float healthPercent = _health / _maxHealth;
        float injured = Mathf.InverseLerp(0.5f, 0f, healthPercent);

        _animator.SetLayerWeight(_injuredLayerIndex, injured);

        OnDamaged?.Invoke();

        Debug.Log($"플레이어가 맞음");
        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러짐");
            GameManager.Instance.GameOver();
        }
    }
}