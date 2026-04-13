using TMPro;
using UnityEngine;

public class LeaderboardItem : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI idText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI killText;

    public void SetData(int rank, string playerId, string playerName, long killCount)
    {
        if (rankText != null) rankText.text = rank.ToString();
        if (idText != null) idText.text = playerId;
        if (nameText != null) nameText.text = playerName;
        if (killText != null) killText.text = killCount.ToString();
    }
}