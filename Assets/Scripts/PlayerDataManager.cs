using System.Collections.Generic;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    // 静态缓存，确保即使实例被销毁也能访问
    private static Dictionary<string, object> cachedPlayerData;
    private static string cachedUserId;

    public Dictionary<string, object> CurrentPlayerData
    {
        get => cachedPlayerData;
        set => cachedPlayerData = value;
    }
    public string CurrentUserId
    {
        get => cachedUserId;
        set => cachedUserId = value;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}