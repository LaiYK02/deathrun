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

        // Listen for network connection events.
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
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
            SetStatus("Connected as Host");

            multiplayerPanel.SetActive(false);
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
    // CONNECTION CALLBACK
    // =========================================================

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
            return;

        // Only handle the local player's connection.
        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        SetStatus("Connected");

        // Hide the multiplayer menu after successful connection.
        if (multiplayerPanel != null)
        {
            multiplayerPanel.SetActive(false);
        }

        Debug.Log(
            $"Connected successfully. Client ID: {clientId}"
        );
    }

    // =========================================================
    // DISCONNECT CALLBACK
    // =========================================================

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
            return;

        // Only handle the local player's disconnection.
        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        SetStatus("Disconnected");

        if (multiplayerPanel != null)
        {
            multiplayerPanel.SetActive(true);
        }

        Debug.Log(
            $"Disconnected. Client ID: {clientId}"
        );
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = "Status: " + message;
        }
    }
}