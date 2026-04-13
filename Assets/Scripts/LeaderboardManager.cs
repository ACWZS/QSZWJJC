using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LeaderboardManager : MonoBehaviour
{
    public GameObject leaderboardPanel;
    public Transform contentParent;
    public GameObject itemPrefab;            // 预制体上必须挂载 LeaderboardItem 脚本
    public Button refreshButton;

    private void Start()
    {
        leaderboardPanel.SetActive(false);
        if (refreshButton != null)
            refreshButton.onClick.AddListener(() => StartCoroutine(RefreshLeaderboard()));
    }

    public void ShowLeaderboard()
    {
        leaderboardPanel.SetActive(true);
        StartCoroutine(RefreshLeaderboard());
    }

    public void CloseLeaderboard()
    {
        leaderboardPanel.SetActive(false);
    }

    private IEnumerator RefreshLeaderboard()
    {
        // 清空列表
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        const int pageSize = 100; // 每页数量，可调整，最大1000
        int currentSkip = 0;
        List<Dictionary<string, object>> allPlayers = new List<Dictionary<string, object>>();

        // 循环分页拉取
        while (true)
        {
            bool querySuccess = false;
            List<Dictionary<string, object>> pageResult = null;
            yield return CloudBaseHttpClient.QueryWithPage("players", null, currentSkip, pageSize, (success, result) =>
            {
                querySuccess = success;
                pageResult = result;
            });

            if (!querySuccess || pageResult == null || pageResult.Count == 0)
                break;

            allPlayers.AddRange(pageResult);
            currentSkip += pageSize;
            yield return null;
        }

        if (allPlayers.Count == 0)
        {
            Debug.LogWarning("未能获取排行榜数据");
            yield break;
        }

        // 按击杀数降序排序
        allPlayers.Sort((a, b) =>
        {
            long killA = a.ContainsKey("killCount") ? System.Convert.ToInt64(a["killCount"]) : 0;
            long killB = b.ContainsKey("killCount") ? System.Convert.ToInt64(b["killCount"]) : 0;
            return killB.CompareTo(killA);
        });

        int rank = 1;
        foreach (var player in allPlayers)
        {
            GameObject item = Instantiate(itemPrefab, contentParent);
            LeaderboardItem itemScript = item.GetComponent<LeaderboardItem>();
            if (itemScript == null)
            {
                Debug.LogError("预制体缺少 LeaderboardItem 组件！");
                continue;
            }

            string playerId = player.ContainsKey("playerId") ? player["playerId"].ToString() : "?";
            string playerName = player.ContainsKey("playerName") ? player["playerName"].ToString() : "?";
            long killCount = player.ContainsKey("killCount") ? System.Convert.ToInt64(player["killCount"]) : 0;

            itemScript.SetData(rank, playerId, playerName, killCount);
            rank++;
        }
    }
}