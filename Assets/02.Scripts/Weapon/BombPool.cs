using System.Collections.Generic;
using UnityEngine;

public class BombPool : MonoBehaviour
{
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private int _initialSize = 10;

    private Queue<Bomb> _pool = new Queue<Bomb>();

    private void Awake()
    {
        // 처음에 미리 만들어서 꺼둔 채로 줄 세우기
        for (int i = 0; i < _initialSize; i++)
        {
            Bomb bomb = CreateBomb();
            bomb.gameObject.SetActive(false);
            _pool.Enqueue(bomb);
        }
    }

    // 꺼내기
    public Bomb Get(Vector3 position)
    {
        // 줄이 비었으면 새로 하나 만들기 (모자랄 때 대비)
        Bomb bomb = _pool.Count > 0 ? _pool.Dequeue() : CreateBomb();

        bomb.transform.position = position;
        bomb.gameObject.SetActive(true);
        return bomb;
    }

    // 반납하기
    public void Return(Bomb bomb)
    {
        bomb.gameObject.SetActive(false);
        _pool.Enqueue(bomb);
    }

    private Bomb CreateBomb()
    {
        Bomb bomb = Instantiate(_bombPrefab, transform); // 풀 아래에 자식으로 정리
        bomb.Init(this);
        return bomb;
    }
}