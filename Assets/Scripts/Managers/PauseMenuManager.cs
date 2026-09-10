using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenu;

    [Header("HUD")]
    [SerializeField] private GameObject hud;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button exitButton;

    [Header("Settings")]
    [SerializeField] private SettingsMenuManager settingsMenuManager;

    private bool isPaused = false;

    private PlayerMovement localPlayerMovement;
    private PlayerLookManager localPlayerLook;

    public bool IsPaused => isPaused;

    private void Start()
    {
        // Make sure the pause menu starts hidden.
        isPaused = false;

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }

        if (hud != null)
        {
            hud.SetActive(true);
        }

        if (settingsMenuManager != null)
        {
            settingsMenuManager.OnSettingsClosed += OnSettingsClosed;
        }

        // ---------------------------------------------------------
        // BUTTONS
        // ---------------------------------------------------------

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(BackToMainMenu);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitToDesktop);
        }

        // Find the local player.
        InvokeRepeating(
            nameof(TryFindLocalPlayer),
            0.1f,
            0.2f
        );
    }

    private void OnDestroy()
    {
        if (settingsMenuManager != null)
        {
            settingsMenuManager.OnSettingsClosed -= OnSettingsClosed;
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(BackToMainMenu);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitToDesktop);
        }

        CancelInvoke(nameof(TryFindLocalPlayer));

        UnlockCursor();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePauseMenu();
        }
    }

    // =========================================================
    // FIND LOCAL PLAYER
    // =========================================================

    private void TryFindLocalPlayer()
    {
        if (localPlayerMovement != null)
        {
            CancelInvoke(nameof(TryFindLocalPlayer));
            return;
        }

        if (Unity.Netcode.NetworkManager.Singleton == null)
            return;

        if (!Unity.Netcode.NetworkManager.Singleton.IsClient)
            return;

        Unity.Netcode.NetworkObject player =
            Unity.Netcode.NetworkManager.Singleton
                .LocalClient?
                .PlayerObject;

        if (player == null)
            return;

        localPlayerMovement =
            player.GetComponent<PlayerMovement>();

        localPlayerLook =
            player.GetComponent<PlayerLookManager>();

        if (localPlayerMovement != null)
        {
            Debug.Log("PauseMenuManager: Local player found.");
            CancelInvoke(nameof(TryFindLocalPlayer));
        }
    }

    // =========================================================
    // TOGGLE
    // =========================================================

    private void TogglePauseMenu()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            OpenPauseMenu();
        }
    }

    // =========================================================
    // OPEN
    // =========================================================

    public void OpenPauseMenu()
    {
        if (isPaused)
            return;

        isPaused = true;

        // Show pause menu.
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }

        // Hide gameplay HUD.
        if (hud != null)
        {
            hud.SetActive(false);
        }

        // Disable local player controls.
        SetPlayerInputEnabled(false);

        // Unlock mouse for UI.
        UnlockCursor();

        Debug.Log("PauseMenuManager: Pause menu opened.");
    }

    private void OnSettingsClosed()
    {
        if (!isPaused)
            return;

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }

        // Keep HUD hidden.
        if (hud != null)
        {
            hud.SetActive(false);
        }

        // Keep gameplay controls disabled.
        SetPlayerInputEnabled(false);

        UnlockCursor();
    }

    // =========================================================
    // RESUME
    // =========================================================

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;

        // Hide pause menu.
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }

        // Show gameplay HUD.
        if (hud != null)
        {
            hud.SetActive(true);
        }

        // Enable local player controls.
        SetPlayerInputEnabled(true);

        // Lock mouse back to gameplay.
        LockCursor();

        Debug.Log("PauseMenuManager: Game resumed.");
    }

    // =========================================================
    // PLAYER INPUT
    // =========================================================

    private void SetPlayerInputEnabled(bool enabled)
    {
        if (localPlayerMovement != null)
        {
            localPlayerMovement.enabled = enabled;
        }

        if (localPlayerLook != null)
        {
            localPlayerLook.enabled = enabled;
        }
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    public void OpenSettings()
    {
        if (settingsMenuManager == null)
        {
            Debug.LogError(
                "PauseMenuManager: SettingsMenuManager is not assigned."
            );

            return;
        }

        // Hide pause menu while settings are open.
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }

        settingsMenuManager.OpenSettings();
    }

    // =========================================================
    // BACK TO MAIN MENU
    // =========================================================

    public void BackToMainMenu()
    {
        Debug.Log(
            "PauseMenuManager: Returning to Main Menu."
        );

        // Make sure gameplay is not left locally disabled.
        isPaused = false;

        // Shut down the multiplayer connection.
        if (NetworkSessionManager.Instance != null)
        {
            NetworkSessionManager.Instance.Shutdown();
        }

        // Load Main Menu.
        SceneManager.LoadScene("MainMenu");
    }

    // =========================================================
    // EXIT
    // =========================================================

    public void ExitToDesktop()
    {
        Debug.Log(
            "PauseMenuManager: Exiting game."
        );

        // Shut down multiplayer before quitting.
        if (NetworkSessionManager.Instance != null)
        {
            NetworkSessionManager.Instance.Shutdown();
        }

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // =========================================================
    // CURSOR
    // =========================================================

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}