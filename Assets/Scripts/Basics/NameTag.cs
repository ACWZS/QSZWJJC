using UnityEngine;
using TMPro;
using Photon.Pun;

public class NameTag : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 0.8f, 0);

    [Header("UI 组件")]
    public TextMeshProUGUI nameText;

    private Camera mainCamera;
    private string displayName;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogError("未找到主相机！");

        if (target == null)
            target = transform.parent;

        // 获取玩家名称
        displayName = GetPlayerName();
        if (nameText != null)
            nameText.text = displayName;
    }

    void LateUpdate()
    {
        if (target != null && mainCamera != null)
        {
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180, 0);
            transform.position = target.position + offset;
        }
    }

    private string GetPlayerName()
    {
        // 1. 联机模式：从 PhotonView 获取
        PhotonView pv = GetComponentInParent<PhotonView>();
        if (pv != null && pv.Owner != null && !string.IsNullOrEmpty(pv.Owner.NickName))
            return pv.Owner.NickName;

        // 2. 单机模式：从 PhotonNetwork.NickName 获取（登录时已设置）
        if (!string.IsNullOrEmpty(PhotonNetwork.NickName))
            return PhotonNetwork.NickName;

        // 3. 单机模式：从 PlayerDataManager 获取
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.CurrentPlayerData != null)
        {
            if (PlayerDataManager.Instance.CurrentPlayerData.ContainsKey("playerName"))
                return PlayerDataManager.Instance.CurrentPlayerData["playerName"].ToString();
        }

        // 4. 默认名称
        return "Player";
    }
}