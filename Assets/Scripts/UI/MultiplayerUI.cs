using UnityEngine;
using TMPro;
using Unity.Netcode;

public class MultiplayerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject multiplayerPanel;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_Text statusText;

    [Header("Network")]
    [SerializeField] private NetworkSessionManager networkSessionManager;

    private void Start()
    {
        // Default IP for testing on the same computer.
        if (ipInputField != null)
        {
            ipInputField.text = "127.0.0.1";
        }

        SetStatus("Disconnected");

        // Menu is visible when starting.
        ShowMenuCursor();
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsConnectedClient)
            return;

        if (multiplayerPanel != null &&
            multiplayerPanel.activeSelf)
            return;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // =========================================================
    // CURSOR
    // =========================================================

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

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsHost)
        {
            EnterGameplay();
        }
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

        string ipAddress = ipInputField.text.Trim();

        if (string.IsNullOrEmpty(ipAddress))
        {
            SetStatus("Please enter a host IP.");
            return;
        }

        SetStatus("Connecting...");

        networkSessionManager.SetHostAddress(ipAddress);
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
        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        SetStatus("Connected");

        EnterGameplay();

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
        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        SetStatus("Disconnected");

        if (multiplayerPanel != null)
        {
            multiplayerPanel.SetActive(true);
        }

        ShowMenuCursor();

        Debug.Log(
            $"Disconnected. Client ID: {clientId}"
        );
    }

    // =========================================================
    // ENTER GAMEPLAY
    // =========================================================

    private void EnterGameplay()
    {
        if (multiplayerPanel != null)
        {
            multiplayerPanel.SetActive(false);
        }

        HideGameplayCursor();

        // Make sure the game window receives mouse focus.
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // =========================================================
    // STATUS
    // =========================================================

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = "Status: " + message;
        }
    }
}