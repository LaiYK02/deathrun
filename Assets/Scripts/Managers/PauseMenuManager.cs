using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

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
    private bool isSettingsOpen = false;

    private PlayerMovement localPlayerMovement;
    private PlayerLookManager localPlayerLook;

    public bool IsPaused => isPaused;

    public bool IsSettingsOpen => isSettingsOpen;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // SINGLETON
        // -----------------------------------------------------

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // -----------------------------------------------------
        // INITIAL STATE
        // -----------------------------------------------------

        isPaused = false;
        isSettingsOpen = false;

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }

        if (hud != null)
        {
            hud.SetActive(true);
        }

        // -----------------------------------------------------
        // SETTINGS EVENT
        // -----------------------------------------------------

        if (settingsMenuManager != null)
        {
            settingsMenuManager.OnSettingsClosed +=
                OnSettingsClosed;
        }

        // -----------------------------------------------------
        // BUTTONS
        // -----------------------------------------------------

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
            mainMenuButton.onClick.AddListener(
                BackToMainMenu
            );
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(
                ExitToDesktop
            );
        }

        // -----------------------------------------------------
        // FIND LOCAL PLAYER
        // -----------------------------------------------------

        InvokeRepeating(
            nameof(TryFindLocalPlayer),
            0.1f,
            0.2f
        );
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (settingsMenuManager != null)
        {
            settingsMenuManager.OnSettingsClosed -=
                OnSettingsClosed;
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(
                ResumeGame
            );
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(
                OpenSettings
            );
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(
                BackToMainMenu
            );
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(
                ExitToDesktop
            );
        }

        CancelInvoke(nameof(TryFindLocalPlayer));

        // Make sure camera control is restored.
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.SetCameraControlEnabled(
                true
            );
        }

        UnlockCursor();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (isSettingsOpen)
        {
            CloseSettingsAndResume();
            return;
        }

        // -----------------------------------------------------
        // PAUSE MENU
        // -----------------------------------------------------

        TogglePauseMenu();
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

        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsClient)
            return;

        NetworkObject player =
            NetworkManager.Singleton
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
            Debug.Log(
                "PauseMenuManager: Local player found."
            );

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

    private void CloseSettingsAndResume()
    {
        if (!isSettingsOpen)
            return;

        Debug.Log(
            "PauseMenuManager: ESC pressed in Settings. " +
            "Closing Settings and resuming game."
        );

        if (settingsMenuManager != null)
        {
            settingsMenuManager.CloseSettings();
        }

        ResumeGame();
    }

    // =========================================================
    // OPEN PAUSE MENU
    // =========================================================

    public void OpenPauseMenu()
    {
        if (isPaused)
            return;

        isPaused = true;
        isSettingsOpen = false;

        // -----------------------------------------------------
        // SHOW PAUSE MENU
        // -----------------------------------------------------

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }

        // -----------------------------------------------------
        // HIDE HUD
        // -----------------------------------------------------

        if (hud != null)
        {
            hud.SetActive(false);
        }

        // -----------------------------------------------------
        // DISABLE LOCAL PLAYER CONTROL
        // -----------------------------------------------------

        SetPlayerInputEnabled(false);

        // -----------------------------------------------------
        // FREEZE CAMERA CONTROL
        // -----------------------------------------------------

        SetCameraControlEnabled(false);

        // -----------------------------------------------------
        // SHOW MOUSE
        // -----------------------------------------------------

        UnlockCursor();

        Debug.Log(
            "PauseMenuManager: Pause menu opened."
        );
    }

    // =========================================================
    // SETTINGS CLOSED
    // =========================================================

    private void OnSettingsClosed()
    {
        if (!isPaused)
            return;

        // Settings has closed.
        isSettingsOpen = false;

        // -----------------------------------------------------
        // SHOW PAUSE MENU AGAIN
        // -----------------------------------------------------

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }

        // -----------------------------------------------------
        // KEEP HUD HIDDEN
        // -----------------------------------------------------

        if (hud != null)
        {
            hud.SetActive(false);
        }

        // -----------------------------------------------------
        // KEEP PLAYER CONTROL DISABLED
        // -----------------------------------------------------

        SetPlayerInputEnabled(false);

        // -----------------------------------------------------
        // KEEP CAMERA FROZEN
        // -----------------------------------------------------

        SetCameraControlEnabled(false);

        // -----------------------------------------------------
        // KEEP MOUSE AVAILABLE FOR UI
        // -----------------------------------------------------

        UnlockCursor();

        Debug.Log(
            "PauseMenuManager: Settings closed. " +
            "Returning to pause menu."
        );
    }

    // =========================================================
    // RESUME
    // =========================================================

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        isSettingsOpen = false;

        // -----------------------------------------------------
        // HIDE PAUSE MENU
        // -----------------------------------------------------

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }

        // -----------------------------------------------------
        // SHOW HUD
        // -----------------------------------------------------

        if (hud != null)
        {
            hud.SetActive(true);
        }

        // -----------------------------------------------------
        // ENABLE LOCAL PLAYER CONTROL
        // -----------------------------------------------------

        SetPlayerInputEnabled(true);

        // -----------------------------------------------------
        // ENABLE CAMERA CONTROL
        // -----------------------------------------------------

        SetCameraControlEnabled(true);

        // -----------------------------------------------------
        // LOCK MOUSE
        // -----------------------------------------------------

        LockCursor();

        Debug.Log(
            "PauseMenuManager: Game resumed."
        );
    }

    // =========================================================
    // PLAYER INPUT
    // =========================================================

    private void SetPlayerInputEnabled(bool enabled)
    {
        RespawnManager respawnManager = null;

        if (NetworkManager.Singleton != null)
        {
            NetworkObject player =
                NetworkManager.Singleton
                    .LocalClient?
                    .PlayerObject;

            if (player != null)
            {
                respawnManager =
                    player.GetComponent<RespawnManager>();
            }
        }

        // -----------------------------------------------------
        // DEAD PLAYER
        // -----------------------------------------------------

        if (respawnManager != null &&
            respawnManager.IsDead.Value)
        {
            if (localPlayerMovement != null)
            {
                localPlayerMovement
                    .SetMovementControlEnabled(false);
            }

            if (localPlayerLook != null)
            {
                localPlayerLook.enabled = false;
            }

            return;
        }

        // -----------------------------------------------------
        // NORMAL PLAYER
        // -----------------------------------------------------

        if (localPlayerMovement != null)
        {
            localPlayerMovement
                .SetMovementControlEnabled(enabled);
        }

        if (localPlayerLook != null)
        {
            localPlayerLook.enabled = enabled;
        }
    }

    // =========================================================
    // CAMERA CONTROL
    // =========================================================

    private void SetCameraControlEnabled(bool enabled)
    {
        if (CameraManager.Instance == null)
            return;

        CameraManager.Instance.SetCameraControlEnabled(
            enabled
        );
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    public void OpenSettings()
    {
        if (!isPaused)
            return;

        if (settingsMenuManager == null)
        {
            Debug.LogError(
                "PauseMenuManager: " +
                "SettingsMenuManager is not assigned."
            );

            return;
        }

        // -----------------------------------------------------
        // MARK SETTINGS AS OPEN
        // -----------------------------------------------------

        isSettingsOpen = true;

        // -----------------------------------------------------
        // HIDE PAUSE MENU
        // -----------------------------------------------------

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }

        // -----------------------------------------------------
        // KEEP HUD HIDDEN
        // -----------------------------------------------------

        if (hud != null)
        {
            hud.SetActive(false);
        }

        // -----------------------------------------------------
        // KEEP PLAYER DISABLED
        // -----------------------------------------------------

        SetPlayerInputEnabled(false);

        // -----------------------------------------------------
        // KEEP CAMERA FROZEN
        // -----------------------------------------------------

        SetCameraControlEnabled(false);

        // -----------------------------------------------------
        // OPEN SETTINGS
        // -----------------------------------------------------

        settingsMenuManager.OpenSettings();

        // -----------------------------------------------------
        // MOUSE REMAINS UNLOCKED
        // -----------------------------------------------------

        UnlockCursor();

        Debug.Log(
            "PauseMenuManager: Settings opened."
        );
    }

    // =========================================================
    // BACK TO MAIN MENU
    // =========================================================

    public void BackToMainMenu()
    {
        Debug.Log(
            "PauseMenuManager: " +
            "Returning to Main Menu."
        );

        isPaused = false;
        isSettingsOpen = false;

        // Shut down multiplayer.
        if (NetworkSessionManager.Instance != null)
        {
            NetworkSessionManager.Instance.Shutdown();
        }

        // Load Main Menu.
        SceneManager.LoadScene("MainMenu");
    }

    // =========================================================
    // EXIT TO DESKTOP
    // =========================================================

    public void ExitToDesktop()
    {
        Debug.Log(
            "PauseMenuManager: Exiting game."
        );

        // Shut down multiplayer.
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