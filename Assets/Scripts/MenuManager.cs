using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void Exit()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void StartTest()
    {
        SceneManager.LoadScene("Arena");
    }

    public void JoinArena()
    {
        SceneManager.LoadScene("ArenaLobby");
    }

    public void ExitLogin()
    {
        // 清除保存的登录凭据，下次进入登录界面将不再自动登录
        PlayerPrefs.DeleteKey("SavedUsername");
        PlayerPrefs.DeleteKey("SavedPassword");
        PlayerPrefs.Save();

        SceneManager.LoadScene("Login");
    }
}