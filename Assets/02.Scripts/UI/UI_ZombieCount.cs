using UnityEngine;
using TMPro;

public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private ZombieSpawner _zombieSpawner;
    private TextMeshProUGUI _text;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _zombieSpawner.OnZombieCountChanged += RefreshZombieCount;
    }

    private void RefreshZombieCount(int maxCount, int count)
    {
        _text.text = $"남은 좀비: {count}마리 / 최대 좀비: {maxCount}마리";
    }
}