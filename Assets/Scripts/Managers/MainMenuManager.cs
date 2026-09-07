using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private GameObject menuPanel;

    [Header("Settings")]
    [SerializeField] private SettingsMenuManager settingsMenuManager;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (settingsMenuManager != null)
        {
            settingsMenuManager.OnSettingsClosed += OnSettingsClosed;
        }
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (settingsMenuManager != null)
        {
            settingsMenuManager.OnSettingsClosed -= OnSettingsClosed;
        }
    }

    // =========================================================
    // START GAME
    // =========================================================

    public void StartGame()
    {
        SceneManager.LoadScene("Lobby");
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    public void Settings()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        if (settingsMenuManager != null)
        {
            settingsMenuManager.OpenSettings();
        }
    }

    // =========================================================
    // SETTINGS CLOSED
    // =========================================================

    private void OnSettingsClosed()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
    }

    // =========================================================
    // CLOSE SETTINGS
    // =========================================================

    public void CloseSettings()
    {
        if (settingsMenuManager != null)
        {
            settingsMenuManager.CloseSettings();
        }
    }

    // =========================================================
    // QUIT
    // =========================================================

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}