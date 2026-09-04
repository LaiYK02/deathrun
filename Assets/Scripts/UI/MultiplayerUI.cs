using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MultiplayerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject multiplayerPanel;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_Text statusText;

    [Header("Network")]
    [SerializeField] private NetworkSessionManager networkSessionManager;

    [Header("Gameplay Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // Default IP for testing on the same computer.
        if (ipInputField != null)
        {
            ipInputField.text = "127.0.0.1";
        }

        SetStatus("Disconnected");

        UpdateCursorForCurrentScene();
    }

    // =========================================================
    // ENABLE
    // =========================================================

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback +=
                OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback +=
                OnClientDisconnected;
        }
    }

    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -=
                OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback -=
                OnClientDisconnected;
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Cursor should only be locked during gameplay.
        UpdateCursorForCurrentScene();
    }

    // =========================================================
    // SCENE LOADED
    // =========================================================

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        UpdateCursorForCurrentScene();
    }

    // =========================================================
    // CURSOR
    // =========================================================

    private void UpdateCursorForCurrentScene()
    {
        if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            HideGameplayCursor();
        }
        else
        {
            ShowMenuCursor();
        }
    }

    private void ShowMenuCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void HideGameplayCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // =========================================================
    // HOST
    // =========================================================

    public void HostGame()
    {
        if (networkSessionManager == null)
        {
            SetStatus("Network manager not assigned.");
            return;
        }

        SetStatus("Starting host...");

        networkSessionManager.StartHost();

        // DO NOT call EnterGameplay() here.
        //
        // We are still in the Lobby.
        // The cursor must remain visible.
    }

    // =========================================================
    // CLIENT
    // =========================================================

    public void JoinGame()
    {
        if (networkSessionManager == null)
        {
            SetStatus("Network manager not assigned.");
            return;
        }

        if (ipInputField == null)
        {
            SetStatus("IP input field not assigned.");
            return;
        }

        string ipAddress =
            ipInputField.text.Trim();

        if (string.IsNullOrEmpty(ipAddress))
        {
            SetStatus("Please enter a host IP.");
            return;
        }

        SetStatus("Connecting...");

        networkSessionManager.SetHostAddress(
            ipAddress
        );

        networkSessionManager.StartClient();
    }

    // =========================================================
    // CONNECTION
    // =========================================================

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
            return;

        // Only react to this computer's own connection.
        if (clientId !=
            NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        SetStatus("Connected");

        // DO NOT lock cursor here.
        //
        // We are still in Lobby.
        // The cursor will be locked automatically
        // when GameScene loads.

        UpdateCursorForCurrentScene();

        Debug.Log(
            $"Connected successfully. Client ID: {clientId}"
        );
    }

    // =========================================================
    // DISCONNECT
    // =========================================================

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
            return;

        // Only react to this computer's own disconnection.
        if (clientId !=
            NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        SetStatus("Disconnected");

        if (multiplayerPanel != null)
        {
            multiplayerPanel.SetActive(true);
        }

        UpdateCursorForCurrentScene();

        Debug.Log(
            $"Disconnected. Client ID: {clientId}"
        );
    }

    // =========================================================
    // STATUS
    // =========================================================

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text =
                "Status: " + message;
        }
    }
}