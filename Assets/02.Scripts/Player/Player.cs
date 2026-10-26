using System;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f;

    private float _health;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    // Action 자료형: 함수(들)을 저장할 수 있다.
    public event Action OnDamaged;

    private void Start()
    {
        _health = _maxHealth;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f)
        {
            return;
        }

        _health -= damageInfo.Value;

        OnDamaged?.Invoke();

        Debug.Log($"플레이어가 맞음");
        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러짐");
            GameManager.Instance.GameOver();
        }
    }
}