using UnityEngine;

/// <summary>
/// 제네릭 기반의 싱글톤 추상 클래스. 
/// 프로젝트 전역에서 단일 인스턴스 유지를 보장하며 씬 전환 시 파괴되지 않음.
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        GameObject singletonObj = new GameObject(typeof(T).Name);
                        _instance = singletonObj.AddComponent<T>();
                        // 부모 객체가 있으면 DontDestroyOnLoad에서 에러가 발생하므로 루트로 이동
                        singletonObj.transform.SetParent(null);
                        DontDestroyOnLoad(singletonObj);
                    }
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject); // 중복 인스턴스 파괴
        }
    }
}