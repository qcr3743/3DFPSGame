using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f;

    [SerializeField] private UI_HitEffect _uiHitEffect;
    private float _health;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

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

        _uiHitEffect.Flash();

        Debug.Log($"플레이어가 맞음");
        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러짐");
            // Todo. 게임 오버
        }
    }
}