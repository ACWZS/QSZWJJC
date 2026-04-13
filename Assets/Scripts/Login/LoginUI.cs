using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public GameObject loginPanel;
    public GameObject messagePanel;
    public TextMeshProUGUI messageText;
    public Button closeButton; // 叉号按钮，拖拽赋值

    private enum Mode { Login, Register }
    private Mode currentMode;

    private const string PREFS_USERNAME = "SavedUsername";
    private const string PREFS_PASSWORD = "SavedPassword";

    void Start()
    {
        loginPanel.SetActive(false);
        messagePanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClosePanel);

        // 尝试自动登录
        TryAutoLogin();
    }

    public void OnLoginButtonClick() { currentMode = Mode.Login; loginPanel.SetActive(true); }
    public void OnRegisterButtonClick() { currentMode = Mode.Register; loginPanel.SetActive(true); }

    public void OnSubmitButtonClick()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;
        if (currentMode == Mode.Login)
            StartCoroutine(Login(username, password));
        else
            StartCoroutine(Register(username, password));
    }

    public void OnClosePanel()
    {
        loginPanel.SetActive(false);
        usernameInput.text = "";
        passwordInput.text = "";
    }

    // ========== 自动登录核心逻辑 ==========
    private void TryAutoLogin()
    {
        if (PlayerPrefs.HasKey(PREFS_USERNAME) && PlayerPrefs.HasKey(PREFS_PASSWORD))
        {
            string username = PlayerPrefs.GetString(PREFS_USERNAME);
            string password = PlayerPrefs.GetString(PREFS_PASSWORD);
            StartCoroutine(AutoLogin(username, password));
        }
    }

    private IEnumerator AutoLogin(string username, string password)
    {
        ShowMessage("正在自动登录...");

        var where = new Dictionary<string, object>
        {
            { "username", username },
            { "password", password }
        };

        bool loginSuccess = false;
        string userId = null;

        yield return CloudBaseHttpClient.Query("users", where, (success, result) =>
        {
            if (success && result.Count > 0)
            {
                var userDoc = result[0];
                userId = userDoc["_id"].ToString();
                PlayerDataManager.Instance.CurrentUserId = userId;
                loginSuccess = true;
            }
        });

        if (loginSuccess)
        {
            // 保存凭据（确保是最新密码）
            SaveCredentials(username, password);
            // 加载玩家扩展数据并跳转
            yield return StartCoroutine(LoadPlayerData(userId));
        }
        else
        {
            ShowMessage("自动登录失败，请手动登录");
            ClearSavedCredentials();
        }
    }

    private void SaveCredentials(string username, string password)
    {
        PlayerPrefs.SetString(PREFS_USERNAME, username);
        PlayerPrefs.SetString(PREFS_PASSWORD, password);
        PlayerPrefs.Save();
    }

    private void ClearSavedCredentials()
    {
        PlayerPrefs.DeleteKey(PREFS_USERNAME);
        PlayerPrefs.DeleteKey(PREFS_PASSWORD);
    }

    // ========== 原有登录/注册逻辑（略作修改以保存凭据）==========
    IEnumerator Login(string username, string password)
    {
        var where = new Dictionary<string, object>
        {
            { "username", username },
            { "password", password }
        };
        bool completed = false;
        bool success = false;
        string userId = null;
        yield return CloudBaseHttpClient.Query("users", where, (s, result) =>
        {
            completed = true;
            success = s && result.Count > 0;
            if (success)
            {
                var userDoc = result[0];
                userId = userDoc["_id"].ToString();
                PlayerDataManager.Instance.CurrentUserId = userId;
            }
        });

        if (!completed) yield break;

        if (success)
        {
            // 登录成功，保存凭据
            SaveCredentials(username, password);
            loginPanel.SetActive(false);
            StartCoroutine(LoadPlayerData(userId));
        }
        else
        {
            ShowMessage("账号或密码错误，请重试。");
        }
    }

    IEnumerator Register(string username, string password)
    {
        // 1. 客户端验证
        if (!Regex.IsMatch(username, @"^\d{8,11}$"))
        {
            ShowMessage("账号必须为8-11位数字！");
            yield break;
        }
        if (!Regex.IsMatch(password, @"^[a-zA-Z0-9]{6,10}$"))
        {
            ShowMessage("密码必须为6-10位英文或数字！");
            yield break;
        }

        // 2. 检查用户名是否已存在
        bool exists = false;
        yield return CloudBaseHttpClient.Query("users", new Dictionary<string, object> { { "username", username } }, (s, result) =>
        {
            if (s && result.Count > 0) exists = true;
        });
        if (exists)
        {
            ShowMessage("该账号已被注册，请直接登录。");
            yield break;
        }

        // 3. 插入新用户
        var newUser = new Dictionary<string, object>
        {
            { "username", username },
            { "password", password },
            { "registerTime", System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
        };
        string newUserId = null;
        yield return CloudBaseHttpClient.Add("users", newUser, (s, response) =>
        {
            if (s) newUserId = response;
        });

        if (string.IsNullOrEmpty(newUserId))
        {
            ShowMessage("注册失败，请稍后重试。");
            yield break;
        }

        // 4. 获取下一个 playerId
        long playerId = 10010;
        yield return GetNextPlayerId((id) => playerId = id);

        // 5. 在 players 集合中创建扩展数据
        var playerData = new Dictionary<string, object>
        {
            { "userId", newUserId },
            { "playerId", playerId },
            { "playerName", $"玩家_{playerId}" },
            { "killCount", 0 },
            { "shardCount", 10 }
        };
        yield return CloudBaseHttpClient.Add("players", playerData, (s, _) =>
        {
            if (s) Debug.Log("玩家扩展数据已保存");
            else Debug.LogError("保存扩展数据失败");
        });

        ShowMessage("注册成功！请登录。");
        loginPanel.SetActive(false);
        usernameInput.text = "";
        passwordInput.text = "";
    }

    IEnumerator LoadPlayerData(string userId)
    {
        bool completed = false;
        yield return CloudBaseHttpClient.Query("players", new Dictionary<string, object> { { "userId", userId } }, (success, result) =>
        {
            if (success && result.Count > 0)
            {
                PlayerDataManager.Instance.CurrentPlayerData = result[0];
                Debug.Log($"玩家数据加载完成，击杀数: {PlayerDataManager.Instance.CurrentPlayerData["killCount"]}");
            }
            else
            {
                Debug.LogWarning("未找到玩家扩展数据");
            }
            completed = true;
        });
        yield return new WaitUntil(() => completed);
        AfterLoginSuccess();
    }

    private IEnumerator GetNextPlayerId(System.Action<long> callback)
    {
        long maxId = 10009;
        bool done = false;
        yield return CloudBaseHttpClient.GetMaxPlayerId((success, id) =>
        {
            if (success) maxId = id;
            else Debug.LogError("获取最大playerId失败，使用默认值10009");
            done = true;
        });
        yield return new WaitUntil(() => done);
        callback(maxId + 1);
    }

    private void AfterLoginSuccess()
    {
        if (PlayerDataManager.Instance.CurrentPlayerData != null)
        {
            string playerName = PlayerDataManager.Instance.CurrentPlayerData["playerName"].ToString();
            PhotonNetwork.NickName = playerName;
            Debug.Log($"设置 Photon 昵称: {playerName}");
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private void ShowMessage(string msg)
    {
        messageText.text = msg;
        messagePanel.SetActive(true);
        CancelInvoke(nameof(HideMessage));
        Invoke(nameof(HideMessage), 2f);
    }

    private void HideMessage() => messagePanel.SetActive(false);
}