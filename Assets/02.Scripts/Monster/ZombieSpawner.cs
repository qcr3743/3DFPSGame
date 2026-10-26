using System;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private Zombie _zombiePrefab;

    [SerializeField] private float _spawnInterval = 2f;
    [SerializeField] private int _maxZombieCount = 5;

    [SerializeField] private float _minXRange = -15f;
    [SerializeField] private float _maxXRange = 15f;
    [SerializeField] private float _spawnY = 1f;
    [SerializeField] private float _minZRange = 8f;
    [SerializeField] private float _maxZRange = 18f;

    [SerializeField] private float _checkRadius = 0.5f; // 위치 겹침 검사용 공의 반지름

    private float _timer = 0f;

    private int _currentZombieCount;

    public event Action<int, int> OnZombieCountChanged;

    private void Awake()
    {
    }

    private void Start()
    {
        for (int i = 0; i < _maxZombieCount; i++)
        {
            SpawnZombie();
        }

        _timer = 0f;
    }

    private void Update()
    {
        if (_currentZombieCount >= _maxZombieCount)
        {
            return;
        }
        else
        {
            _timer += Time.deltaTime;
            if (_timer > _spawnInterval)
            {
                SpawnZombie();
                _timer = 0f;
            }
        }
    }

    private void SpawnZombie()
    {
        float spawnX = UnityEngine.Random.Range(_minXRange, _maxXRange);
        float spawnZ = UnityEngine.Random.Range(_minZRange, _maxZRange);
        Vector3 spawnPosition = new Vector3(spawnX, _spawnY, spawnZ);

        if (Physics.CheckSphere(spawnPosition, _checkRadius))
        {
            return;
        }


        Zombie zombie = Instantiate(_zombiePrefab, spawnPosition, Quaternion.identity);

        _currentZombieCount++;
        OnZombieCountChanged?.Invoke(_maxZombieCount, _currentZombieCount);
        zombie.OnDied += OnZombieDied;
    }

    private void OnZombieDied()
    {
        _currentZombieCount--;
        OnZombieCountChanged?.Invoke(_maxZombieCount, _currentZombieCount);
    }
}