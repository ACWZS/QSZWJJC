using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using TMPro;

public class RoomItem : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI playerCountText;
    public Button joinButton;
    private string roomName;
    private ArenaUIManager manager;

    public void Setup(RoomInfo room, ArenaUIManager mgr)
    {
        manager = mgr;
        roomName = room.Name;
        roomNameText.text = room.Name;
        playerCountText.text = $"{room.PlayerCount}/{room.MaxPlayers}";
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => manager.JoinRoom(roomName));
    }
}