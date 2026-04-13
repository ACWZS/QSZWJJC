using Photon.Pun;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class GameNetworkManager : MonoBehaviourPunCallbacks
{
    public static GameNetworkManager Instance { get; private set; }  // 添加单例属性

    public GameObject playerPrefab;
    public Transform[] spawnPoints;
    private bool spawned = false;

    void Awake()
    {
        // 单例模式：确保只有一个实例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        //Debug.Log("GameNetworkManager Start: IsConnected=" + PhotonNetwork.IsConnected + ", InRoom=" + PhotonNetwork.InRoom);
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && !spawned)
        {
            SpawnPlayer();
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("OnJoinedRoom: 进入房间，准备生成玩家");
        if (!spawned)
        {
            SpawnPlayer();
        }
    }

    private void SpawnPlayer()
    {
        if (spawned) return;
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("未设置出生点！");
            return;
        }

        int spawnIndex = PhotonNetwork.LocalPlayer.ActorNumber % spawnPoints.Length;
        Vector3 spawnPos = spawnPoints[spawnIndex].position;
        Quaternion spawnRot = spawnPoints[spawnIndex].rotation;

        GameObject playerObj = PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, spawnRot);
        spawned = true;
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        //Debug.Log("=== GameNetworkManager.OnLeftRoom 被调用 ===");

        var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
        if (playerData == null || !playerData.ContainsKey("_id"))
        {
            Debug.LogError("玩家数据未就绪，无法更新击杀数");
            return;
        }

        int kills = PlayerMain.LastMatchKills;
        if (kills == 0)
        {
            //Debug.Log("本局击杀数为0，无需更新");
            return;
        }

        StartCoroutine(UpdateKillCountCoroutine(kills));
    }

    private IEnumerator UpdateKillCountCoroutine(int kills)
    {
        string docId = PlayerDataManager.Instance.CurrentPlayerData["_id"].ToString();
        int currentTotal = 0;
        if (PlayerDataManager.Instance.CurrentPlayerData.ContainsKey("killCount"))
            currentTotal = Convert.ToInt32(PlayerDataManager.Instance.CurrentPlayerData["killCount"]);
        int newTotal = currentTotal + kills;

        //Debug.Log($"准备更新击杀数：本局 {kills}，当前总 {currentTotal}，新总 {newTotal}");

        bool success = false;
        yield return CloudBaseHttpClient.Update(
            "players",
            docId,
            new Dictionary<string, object> { { "killCount", newTotal } },
            (s, response) => {
                success = s;
                //Debug.Log($"更新回调 success={s}, response={response}");
            }
        );

        if (success)
        {
            PlayerDataManager.Instance.CurrentPlayerData["killCount"] = newTotal;
            //Debug.Log($"击杀数已更新至 {newTotal}");
        }
        else
        {
            Debug.LogError("击杀数更新失败");
        }

        PlayerMain.ResetLastMatchKills();
    }
}