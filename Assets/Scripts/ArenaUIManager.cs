using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class ArenaUIManager : MonoBehaviourPunCallbacks
{
    [Header("页面面板")]
    public GameObject lobbyPanel;
    public GameObject roomPanel;

    [Header("大厅界面 UI")]
    public TMP_InputField roomNameInput;
    public Button createRoomButton;
    public Button refreshButton;
    public Transform roomListContent;
    public GameObject roomItemPrefab;

    [Header("房间界面 UI")]
    public Transform playerListContent;
    public GameObject playerListItemPrefab;
    public Button startGameButton;
    public Button leaveRoomButton;
    public TMP_Text roomNameDisplay;   // 显示房间号的 TextMeshPro 组件（拖拽到 Inspector）

    [Header("公用")]
    public Button backButton;

    [Header("提示 UI")]
    public GameObject warningPanel;
    public TMP_Text warningText;

    // 本地维护的房间列表（RoomInfo 可能不完整，但足够展示）
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomListItems = new Dictionary<string, GameObject>();
    private bool isConnected = false;
    private bool isLeaving = false;

    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "asia";
        // 移除重复的 ConnectUsingSettings()
        PhotonNetwork.GameVersion = "1.0";
        PhotonNetwork.AutomaticallySyncScene = true;

        PhotonNetwork.GameVersion = "1.0";
        PhotonNetwork.AutomaticallySyncScene = true;

        createRoomButton.onClick.AddListener(OnCreateRoom);
        refreshButton.onClick.AddListener(OnRefreshRoomList);
        leaveRoomButton.onClick.AddListener(OnLeaveRoom);
        startGameButton.onClick.AddListener(OnStartGame);
        backButton.onClick.AddListener(OnBackToMainMenu);

        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);
        if (warningPanel != null) warningPanel.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        isConnected = true;
        ShowWarning("连接服务器成功！");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        ShowWarning("已加入大厅，可以创建或加入房间");
        // 可选：主动请求一次房间列表
        // PhotonNetwork.GetCustomRoomList(TypedLobby.Default, null);
    }

    // 关键：正确处理房间列表更新（增量）
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
            {
                // 房间被移除（关闭或不可见）
                cachedRoomList.Remove(room.Name);
            }
            else
            {
                // 新增或更新房间信息
                cachedRoomList[room.Name] = room;
            }
        }
        // 基于最新的缓存重建 UI
        RefreshRoomListUI();
    }

    private void RefreshRoomListUI()
    {
        // 清除所有现有 UI 元素
        foreach (var item in roomListItems.Values)
            Destroy(item);
        roomListItems.Clear();

        // 遍历缓存列表，为每个房间创建 UI
        foreach (var kvp in cachedRoomList)
        {
            RoomInfo room = kvp.Value;
            if (room == null || room.RemovedFromList) continue;

            GameObject newItem = Instantiate(roomItemPrefab, roomListContent);
            RoomItem itemScript = newItem.GetComponent<RoomItem>();
            itemScript.Setup(room, this);
            roomListItems[room.Name] = newItem;
        }
    }

    // 手动刷新：清空缓存并请求服务器重新发送完整列表
    private void OnRefreshRoomList()
    {
        cachedRoomList.Clear();
        RefreshRoomListUI();
        // 请求重新获取房间列表（Photon 会触发新的 OnRoomListUpdate）
        PhotonNetwork.GetCustomRoomList(TypedLobby.Default, null);
        ShowWarning("正在刷新房间列表...");
    }

    // 创建房间成功
    public override void OnCreatedRoom()
    {
        ShowWarning("房间创建成功！");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ShowWarning($"创建房间失败：{message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        ShowWarning($"加入房间失败：{message}");
    }

    public override void OnJoinedRoom()
    {
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);
        UpdatePlayerList();
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        // 显示房间号
        if (roomNameDisplay != null)
            roomNameDisplay.text = $"房间号：{PhotonNetwork.CurrentRoom.Name}";
        ShowWarning("已加入房间");
    }

    // 在 OnLeftRoom 中清空房间号显示
    public override void OnLeftRoom()
    {
        isLeaving = false;
        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);
        UpdatePlayerList();
        if (roomNameDisplay != null)
            roomNameDisplay.text = "";
        ChatManager cm = roomPanel.GetComponentInChildren<ChatManager>();
        if (cm != null) cm.ClearMessages();
        OnRefreshRoomList();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        foreach (Transform child in playerListContent)
            Destroy(child.gameObject);

        if (playerListItemPrefab == null)
        {
            Debug.LogError("playerListItemPrefab 未赋值！");
            return;
        }

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject newItem = Instantiate(playerListItemPrefab, playerListContent);
            Text legacyText = newItem.GetComponent<Text>();
            if (legacyText != null)
                legacyText.text = player.NickName;
            else
            {
                TMP_Text tmpText = newItem.GetComponent<TMP_Text>();
                if (tmpText != null)
                    tmpText.text = player.NickName;
                else
                    Debug.LogError("玩家项预制体缺少 Text 或 TMP_Text 组件！");
            }
        }
    }

    private void OnCreateRoom()
    {
        if (!isConnected)
        {
            ShowWarning("尚未连接到服务器，请稍后再试，或返回主菜单重新进入联机大厅");
            return;
        }

        string roomName = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(roomName))
        {
            ShowWarning("房间名不能为空");
            return;
        }

        RoomOptions options = new RoomOptions { MaxPlayers = 8, IsVisible = true, IsOpen = true };
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    private void OnLeaveRoom()
    {
        if (isLeaving) return;
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("当前不在任何房间内，无需退出");
            return;
        }
        isLeaving = true;
        PhotonNetwork.LeaveRoom();
    }

    private void OnStartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            ShowWarning("只有房主可以开始游戏！");
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            ShowWarning("房间人数不足2人，无法开始游戏！");
            return;
        }

        Debug.Log("房主开始游戏，加载地图中");
        PhotonNetwork.LoadLevel("Arena_Online");
    }

    private void OnBackToMainMenu()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        else
            PhotonNetwork.Disconnect();
        SceneManager.LoadScene("MainMenu");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        isLeaving = false;
        isConnected = false;
        ShowWarning($"网络断开，原因：{cause}");
    }

    private void ShowWarning(string message)
    {
        Debug.LogWarning(message);
        if (warningPanel != null && warningText != null)
        {
            warningText.text = message;
            warningPanel.SetActive(true);
            Invoke(nameof(HideWarning), 2f);
        }
    }

    private void HideWarning()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);
    }
}