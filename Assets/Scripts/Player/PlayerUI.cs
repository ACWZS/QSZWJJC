using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance { get; private set; }

    // 武器弹药文本
    public TextMeshProUGUI ammoText0;
    public TextMeshProUGUI ammoText1;
    public TextMeshProUGUI ammoText2;
    public TextMeshProUGUI ammoText3;
    public TextMeshProUGUI ammoText4;

    // 射击按钮 RectTransform
    public RectTransform fireButtonRect0;
    public RectTransform fireButtonRect1;
    public RectTransform fireButtonRect2;
    public RectTransform fireButtonRect3;
    public RectTransform fireButtonRect4;

    // 射击按钮
    public Button fireButton0;
    public Button fireButton1;
    public Button fireButton2;
    public Button fireButton3;
    public Button fireButton4;

    [Header("治疗物品")]
    public Button energyDrinkButton;
    public Button bandageButton;
    public Button medkitButton;
    public TextMeshProUGUI bandageButtonText;
    public TextMeshProUGUI medkitButtonText;
    // 新增：能量饮料按钮的文本组件
    public TextMeshProUGUI energyDrinkButtonText;

    [Header("联机")]
    public Button exitRoomButton;

    [Header("跳跃")]
    public Button jumpButton;

    [Header("玩家信息")]
    public TextMeshProUGUI playerCountText;

    [Header("治疗信息面板")]
    public GameObject infoPanel;           // 信息面板根物体
    public TextMeshProUGUI infoText;       // 显示具体信息的文本

    [Header("聊天系统")]
    public Button chatToggleButton;      // 打开聊天面板的按钮
    public GameObject chatPanel;         // 聊天面板（包含输入框、显示区、发送按钮等）
    public Button closeChatButton;       // 关闭聊天面板的按钮

    private float updateInterval = 0.5f;
    private float lastUpdateTime = 0f;

    [Header("游戏内击杀显示")]
    public TextMeshProUGUI inGameKillCountText;   // 拖拽到 Inspector 中对应文本

    public void UpdateInGameKillCount(int killCount)
    {
        if (inGameKillCountText != null)
            inGameKillCountText.text = $"击杀: {killCount}";
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 绑定治疗按钮事件
        if (energyDrinkButton != null)
            energyDrinkButton.onClick.AddListener(() => PlayerMain.Instance?.UseEnergyDrink());
        if (bandageButton != null)
            bandageButton.onClick.AddListener(() => PlayerMain.Instance?.UseBandage());
        if (medkitButton != null)
            medkitButton.onClick.AddListener(() => PlayerMain.Instance?.UseMedkit());


        // 聊天面板初始化
        if (chatToggleButton != null)
            chatToggleButton.onClick.AddListener(() => chatPanel.SetActive(true));
        if (closeChatButton != null)
            closeChatButton.onClick.AddListener(() => chatPanel.SetActive(false));
        if (chatPanel != null)
            chatPanel.SetActive(false);

        // 绑定退出房间按钮
        if (exitRoomButton != null)
            exitRoomButton.onClick.AddListener(OnExitRoom);

        // 绑定跳跃按钮
        if (jumpButton != null)
        {
            jumpButton.onClick.RemoveAllListeners();
            jumpButton.onClick.AddListener(() => PlayerMain.Instance?.OnJumpPressed());
        }

        // 初始化按钮默认文字
        UpdateBandageButtonText("");
        UpdateMedkitButtonText("");
        UpdateEnergyDrinkButtonText("");   // 新增：初始化能量饮料按钮文字

        // 初始更新玩家数量
        UpdatePlayerCount();

        // 隐藏信息面板
        if (infoPanel != null)
            infoPanel.SetActive(false);

    }

    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdatePlayerCount();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdatePlayerCount()
    {
        if (playerCountText == null) return;

        if (PhotonNetwork.InRoom)
        {
            int playerCount = PhotonNetwork.PlayerList.Length;
            playerCountText.text = $"人数：{playerCount}";
        }
        else
        {
            playerCountText.text = "人数：1";
        }
    }

    // 退出房间方法
    private void OnExitRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        SceneManager.LoadScene("ArenaLobby");
    }

    // ---------- 治疗信息面板控制 ----------
    private Coroutine autoHideCoroutine;

    public void ShowInfo(string message, float autoHideTime = 0f)
    {
        if (infoPanel != null)
        {
            // 取消之前的自动隐藏协程
            if (autoHideCoroutine != null)
                StopCoroutine(autoHideCoroutine);

            infoText.text = message;
            infoPanel.SetActive(true);

            if (autoHideTime > 0f)
            {
                autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(autoHideTime));
            }
        }
    }

    private IEnumerator AutoHideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideInfo();
        autoHideCoroutine = null;
    }

    public void HideInfo()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    // ---------- 以下为原有方法保持不变 ----------
    public void UpdateBandageButtonText(string text)
    {
        if (bandageButtonText != null)
            bandageButtonText.text = text;
    }

    public void UpdateMedkitButtonText(string text)
    {
        if (medkitButtonText != null)
            medkitButtonText.text = text;
    }

    // 能量饮料按钮的默认文字
    public void UpdateEnergyDrinkButtonText(string text)
    {
        if (energyDrinkButtonText != null)
            energyDrinkButtonText.text = text;
    }

    public void UpdateBandageTimer(float remaining)
    {
        if (bandageButtonText != null)
            bandageButtonText.text = remaining > 0 ? $"{remaining:F1}s" : "";
    }

    public void UpdateMedkitTimer(float remaining)
    {
        if (medkitButtonText != null)
            medkitButtonText.text = remaining > 0 ? $"{remaining:F1}s" : "";
    }

    // 更新能量饮料按钮的冷却倒计时显示
    public void UpdateEnergyDrinkTimer(float remaining)
    {
        if (energyDrinkButtonText != null)
        {
            if (remaining > 0)
                energyDrinkButtonText.text = $"{remaining:F1}s";
            else
                UpdateEnergyDrinkButtonText("");   // 冷却结束恢复默认文字
        }
    }

    public void UpdateAllAmmoDisplays()
    {
        var player = PlayerMain.Instance;
        if (player == null) return;

        var weapons = player.weapons;
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponData data = weapons[i];
            if (data == null) continue;

            if (player.WeaponModels.TryGetValue(data.weaponName, out GameObject model))
            {
                Gun gun = model.GetComponent<Gun>();
                if (gun != null)
                {
                    string ammoString = $"{gun.currentAmmo}/{gun.totalAmmo}";
                    UpdateAmmoText(i, ammoString);
                }
            }
        }
    }

    private void UpdateAmmoText(int index, string text)
    {
        switch (index)
        {
            case 0: if (ammoText0 != null) ammoText0.text = text; break;
            case 1: if (ammoText1 != null) ammoText1.text = text; break;
            case 2: if (ammoText2 != null) ammoText2.text = text; break;
            case 3: if (ammoText3 != null) ammoText3.text = text; break;
            case 4: if (ammoText4 != null) ammoText4.text = text; break;
        }
    }

    public void OnReloadButton()
    {
        if (PlayerMain.Instance != null && PlayerMain.Instance.CurrentGun != null)
            PlayerMain.Instance.CurrentGun.TryReload();
    }

    public void SetFireButtonInteractable(int weaponIndex, bool interactable)
    {
        Button btn = null;
        switch (weaponIndex)
        {
            case 0: btn = fireButton0; break;
            case 1: btn = fireButton1; break;
            case 2: btn = fireButton2; break;
            case 3: btn = fireButton3; break;
            case 4: btn = fireButton4; break;
        }
        if (btn != null) btn.interactable = interactable;
    }

    public void SwitchWeapon0() => PlayerMain.Instance?.SwitchToWeapon(0);
    public void SwitchWeapon1() => PlayerMain.Instance?.SwitchToWeapon(1);
    public void SwitchWeapon2() => PlayerMain.Instance?.SwitchToWeapon(2);
    public void SwitchWeapon3() => PlayerMain.Instance?.SwitchToWeapon(3);
    public void SwitchWeapon4() => PlayerMain.Instance?.SwitchToWeapon(4);

    public void OnFireButtonDown_Weapon0() => PlayerMain.Instance?.OnFireButtonDown(fireButtonRect0, 0);
    public void OnFireButtonUp_Weapon0() => PlayerMain.Instance?.OnFireButtonUp();
    public void OnFireButtonDrag_Weapon0() => PlayerMain.Instance?.OnFireButtonDrag();

    public void OnFireButtonDown_Weapon1() => PlayerMain.Instance?.OnFireButtonDown(fireButtonRect1, 1);
    public void OnFireButtonUp_Weapon1() => PlayerMain.Instance?.OnFireButtonUp();
    public void OnFireButtonDrag_Weapon1() => PlayerMain.Instance?.OnFireButtonDrag();

    public void OnFireButtonDown_Weapon2() => PlayerMain.Instance?.OnFireButtonDown(fireButtonRect2, 2);
    public void OnFireButtonUp_Weapon2() => PlayerMain.Instance?.OnFireButtonUp();
    public void OnFireButtonDrag_Weapon2() => PlayerMain.Instance?.OnFireButtonDrag();

    public void OnFireButtonDown_Weapon3() => PlayerMain.Instance?.OnFireButtonDown(fireButtonRect3, 3);
    public void OnFireButtonUp_Weapon3() => PlayerMain.Instance?.OnFireButtonUp();
    public void OnFireButtonDrag_Weapon3() => PlayerMain.Instance?.OnFireButtonDrag();

    public void OnFireButtonDown_Weapon4() => PlayerMain.Instance?.OnFireButtonDown(fireButtonRect4, 4);
    public void OnFireButtonUp_Weapon4() => PlayerMain.Instance?.OnFireButtonUp();
    public void OnFireButtonDrag_Weapon4() => PlayerMain.Instance?.OnFireButtonDrag();
}