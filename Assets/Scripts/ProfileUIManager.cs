using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ProfileUIManager : MonoBehaviour
{
    public TextMeshProUGUI userIdText;      // 显示用户 ID
    public TextMeshProUGUI playerNameText;  // 显示玩家名称
    public TextMeshProUGUI killCountText;   // 显示击杀数
    public Button modifyNameButton;         // 修改名称按钮
    public GameObject modifyNamePanel;      // 修改名称的面板（输入框+确认/取消）
    public TMP_InputField newNameInput;     // 新名称输入框
    public Button confirmButton;            // 确认按钮
    public Button cancelButton;             // 取消按钮

    private void Start()
    {
        // 确保面板初始隐藏
        modifyNamePanel.SetActive(false);

        // 绑定按钮事件
        modifyNameButton.onClick.AddListener(() => modifyNamePanel.SetActive(true));
        confirmButton.onClick.AddListener(OnConfirmModifyName);
        cancelButton.onClick.AddListener(() => modifyNamePanel.SetActive(false));

        // 加载玩家数据
        LoadProfileData();
    }

    private void LoadProfileData()
    {
        if (PlayerDataManager.Instance.CurrentPlayerData == null)
        {
            Debug.LogWarning("未找到玩家数据，请先登录");
            return;
        }
        var data = PlayerDataManager.Instance.CurrentPlayerData;
        // 显示自定义的 playerId 字段（例如 10010）
        userIdText.text = "ID: " + data["playerId"].ToString();
        playerNameText.text = data["playerName"].ToString();
        killCountText.text = "击杀数: " + data["killCount"].ToString();
    }

    public void OnConfirmModifyName()
    {
        StartCoroutine(ModifyNameCoroutine());
    }

    private IEnumerator ModifyNameCoroutine()
    {
        string newName = newNameInput.text.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogError("新名称不能为空");
            yield break;
        }

        bool success = false;
        yield return CloudBaseHttpClient.Update(
            "players",
            PlayerDataManager.Instance.CurrentPlayerData["_id"].ToString(),
            new Dictionary<string, object> { { "playerName", newName } },
            (s, _) => success = s
        );

        if (success)
        {
            PlayerDataManager.Instance.CurrentPlayerData["playerName"] = newName;
            playerNameText.text = newName;
            modifyNamePanel.SetActive(false);
            Debug.Log("名称修改成功");
        }
        else
        {
            Debug.LogError("名称修改失败");
        }
    }
}