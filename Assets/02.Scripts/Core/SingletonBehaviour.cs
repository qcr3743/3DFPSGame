using UnityEngine;

// 마우스 커서를 화면 가운데에 잠그고 숨긴다.
public class SingletonBehaviour<T> : MonoBehaviour where T : class
{
    private static T _instance = null;
    public static T Instance => _instance;

    protected virtual void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
    }
}