using UnityEngine;

public class CloudBaseManager : MonoBehaviour
{
    public static CloudBaseManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // 你不需要额外的初始化，API Key 已写在 HttpClient 中
}