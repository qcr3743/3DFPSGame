using UnityEngine;

// 씬에 하나만 존재하는 컴포넌트를 Instance로 접근할 수 있게 해 주는 싱글톤 베이스 클래스.
// 씬의 UI 등을 참조하는 매니저가 쓰므로 DontDestroyOnLoad를 하지 않는다.
// 씬이 다시 로드되면 이전 인스턴스는 씬과 함께 파괴되고, 새 씬의 인스턴스가 Instance가 된다.
public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance = null;
    public static T Instance => _instance;

    protected virtual void Awake()
    {
        // T가 MonoBehaviour라서 != null은 Unity의 null 판정을 쓴다.
        // 이전 씬에서 파괴된 인스턴스는 null로 취급되므로 새 인스턴스가 정상적으로 등록된다.
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
    }
}
