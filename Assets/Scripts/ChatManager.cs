using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class ChatManager : MonoBehaviour, IOnEventCallback
{
    public TMP_InputField chatInput;
    public TextMeshProUGUI chatDisplay;
    public ScrollRect scrollRect;
    public Button sendButton;

    private const byte ChatEventCode = 1;
    private List<string> messages = new List<string>();

    void Start()
    {
        PhotonNetwork.AddCallbackTarget(this);
        sendButton.onClick.AddListener(OnSendButton);
    }

    void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    void OnSendButton()
    {
        if (string.IsNullOrEmpty(chatInput.text)) return;
        string message = $"{PhotonNetwork.LocalPlayer.NickName}: {chatInput.text}";
        object content = message;
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(ChatEventCode, content, options, SendOptions.SendReliable);
        chatInput.text = "";
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == ChatEventCode)
        {
            string message = (string)photonEvent.CustomData;
            messages.Add(message);
            if (messages.Count > 50) messages.RemoveAt(0);
            UpdateChatDisplay();
        }
    }

    void UpdateChatDisplay()
    {
        if (chatDisplay == null) return;
        chatDisplay.text = string.Join("\n", messages);
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    // 供 ArenaUIManager 调用，清空聊天记录
    public void ClearMessages()
    {
        messages.Clear();
        UpdateChatDisplay();
    }
}