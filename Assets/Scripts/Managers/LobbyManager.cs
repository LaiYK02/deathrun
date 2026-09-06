using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private GameObject playerListPanel;

    [Header("Connection UI")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button connectionBackButton;
    [SerializeField] private TMP_InputField ipInputField;

    [Header("Room UI")]
    [SerializeField] private Button roomBackButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button copyIPButton;
    [SerializeField] private TMP_Text copyIPButtonText;

    [Header("Player List")]
    [SerializeField] private TMP_Text playerListText;

    [Header("Floating Notification")]
    [SerializeField] private FloatingNotificationManager notificationManager;

    [Header("Scenes")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameScene = "GameScene";

    [Header("Connection Settings")]
    [SerializeField] private float connectionTimeout = 10f;

    private NetworkSessionManager networkSessionManager;

    private bool isConnecting = false;
    private bool isLeavingRoom = false;

    private Coroutine connectionTimeoutCoroutine;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // GET NETWORK SESSION MANAGER
        // -----------------------------------------------------

        networkSessionManager =
            NetworkSessionManager.Instance;

        if (networkSessionManager == null)
        {
            Debug.LogError(
                "LobbyManager: NetworkSessionManager was not found."
            );

            return;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError(
                "LobbyManager: NetworkManager.Singleton was not found."
            );

            return;
        }

        if (notificationManager == null)
        {
            Debug.LogWarning(
                "LobbyManager: Floating Notification Manager is not assigned."
            );
        }

        // -----------------------------------------------------
        // BUTTON LISTENERS
        // -----------------------------------------------------

        hostButton.onClick.AddListener(
            OnHostClicked
        );

        joinButton.onClick.AddListener(
            OnJoinClicked
        );

        connectionBackButton.onClick.AddListener(
            OnConnectionBackClicked
        );

        roomBackButton.onClick.AddListener(
            OnRoomBackClicked
        );

        startGameButton.onClick.AddListener(
            OnStartGameClicked
        );

        copyIPButton.onClick.AddListener(
            OnCopyIPClicked
        );

        // -----------------------------------------------------
        // NETWORK CALLBACKS
        // -----------------------------------------------------

        NetworkManager.Singleton.OnClientConnectedCallback +=
            OnClientConnected;

        NetworkManager.Singleton.OnClientDisconnectCallback +=
            OnClientDisconnected;

        // -----------------------------------------------------
        // INITIAL UI
        // -----------------------------------------------------

        ShowConnectionUI();
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        CancelConnectionTimeout();

        if (hostButton != null)
        {
            hostButton.onClick.RemoveListener(
                OnHostClicked
            );
        }

        if (joinButton != null)
        {
            joinButton.onClick.RemoveListener(
                OnJoinClicked
            );
        }

        if (connectionBackButton != null)
        {
            connectionBackButton.onClick.RemoveListener(
                OnConnectionBackClicked
            );
        }

        if (roomBackButton != null)
        {
            roomBackButton.onClick.RemoveListener(
                OnRoomBackClicked
            );
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(
                OnStartGameClicked
            );
        }

        if (copyIPButton != null)
        {
            copyIPButton.onClick.RemoveListener(
                OnCopyIPClicked
            );
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -=
                OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback -=
                OnClientDisconnected;
        }
    }

    // =========================================================
    // HOST
    // =========================================================

    private void OnHostClicked()
    {
        if (networkSessionManager == null)
        {
            notificationManager.ShowNotification(
                "Network manager not found!"
            );

            return;
        }

        if (NetworkManager.Singleton == null)
        {
            notificationManager.ShowNotification(
                "Network manager not found!"
            );

            return;
        }

        if (NetworkManager.Singleton.IsListening)
            return;

        isConnecting = true;
        isLeavingRoom = false;

        SetConnectionButtonsInteractable(
            false
        );

        bool success =
            networkSessionManager.StartHost();

        if (!success)
        {
            isConnecting = false;

            SetConnectionButtonsInteractable(
                true
            );

            notificationManager.ShowNotification(
                "Failed to host!"
            );

            return;
        }

        // Successful host:
        // Do not show notification.
        //
        // OnClientConnected() will enter the room.
    }

    // =========================================================
    // JOIN
    // =========================================================

    private void OnJoinClicked()
    {
        if (networkSessionManager == null)
        {
            notificationManager.ShowNotification(
                "Network manager not found!"
            );

            return;
        }

        if (NetworkManager.Singleton == null)
        {
            notificationManager.ShowNotification(
                "Network manager not found!"
            );

            return;
        }

        string ip =
            ipInputField.text.Trim();

        if (string.IsNullOrEmpty(ip))
        {
            notificationManager.ShowNotification(
                "Enter an IP address!"
            );

            return;
        }

        if (NetworkManager.Singleton.IsListening)
            return;

        networkSessionManager.SetHostAddress(
            ip
        );

        isConnecting = true;
        isLeavingRoom = false;

        SetConnectionButtonsInteractable(
            false
        );

        bool success =
            networkSessionManager.StartClient();

        if (!success)
        {
            isConnecting = false;

            SetConnectionButtonsInteractable(
                true
            );

            notificationManager.ShowNotification(
                "Failed to connect!"
            );

            return;
        }

        // Start connection timeout.
        CancelConnectionTimeout();

        connectionTimeoutCoroutine =
            StartCoroutine(
                ConnectionTimeout()
            );
    }

    // =========================================================
    // CONNECTION TIMEOUT
    // =========================================================

    private IEnumerator ConnectionTimeout()
    {
        yield return new WaitForSeconds(
            connectionTimeout
        );

        connectionTimeoutCoroutine = null;

        if (!isConnecting)
            yield break;

        if (NetworkManager.Singleton == null)
            yield break;

        if (NetworkManager.Singleton.IsConnectedClient)
            yield break;

        Debug.LogWarning(
            "LobbyManager: Connection attempt timed out."
        );

        isConnecting = false;

        networkSessionManager.Shutdown();

        ShowConnectionUI();

        notificationManager.ShowNotification(
            "Connection timed out!"
        );
    }

    // =========================================================
    // CLIENT CONNECTED
    // =========================================================

    private void OnClientConnected(
        ulong clientId)
    {
        Debug.Log(
            $"LobbyManager: Client connected. " +
            $"Client ID = {clientId}"
        );

        if (NetworkManager.Singleton == null)
            return;

        // -----------------------------------------------------
        // HOST
        // -----------------------------------------------------

        if (NetworkManager.Singleton.IsHost)
        {
            // Only enter the room when the host itself finishes connecting.
            if (clientId ==
                NetworkManager.Singleton.LocalClientId)
            {
                CancelConnectionTimeout();

                isConnecting = false;

                EnterRoom(true);
            }

            // If this is another player joining, just refresh the host's list.
            else
            {
                RefreshPlayerList();
            }

            return;
        }

        // -----------------------------------------------------
        // CLIENT
        // -----------------------------------------------------

        if (clientId ==
            NetworkManager.Singleton.LocalClientId)
        {
            CancelConnectionTimeout();

            isConnecting = false;

            EnterRoom(false);
        }
    }

    // =========================================================
    // CLIENT DISCONNECTED
    // =========================================================

    private void OnClientDisconnected(
        ulong clientId)
    {
        Debug.Log(
            $"LobbyManager: Client disconnected. " +
            $"Client ID = {clientId}"
        );

        if (NetworkManager.Singleton == null)
            return;

        // -----------------------------------------------------
        // INTENTIONAL DISCONNECT
        // -----------------------------------------------------

        if (isLeavingRoom)
        {
            return;
        }

        // -----------------------------------------------------
        // CLIENT
        // -----------------------------------------------------

        if (!NetworkManager.Singleton.IsHost)
        {
            if (clientId ==
                NetworkManager.Singleton.LocalClientId)
            {
                isConnecting = false;

                ShowConnectionUI();

                SetConnectionButtonsInteractable(
                    true
                );

                notificationManager.ShowNotification(
                    "Failed to connect!"
                );
            }

            return;
        }

        // -----------------------------------------------------
        // HOST
        // -----------------------------------------------------

        RefreshPlayerList();

        // No notification when another player simply leaves.
    }

    // =========================================================
    // ENTER ROOM
    // =========================================================

    private void EnterRoom(
        bool isHost)
    {
        connectionPanel.SetActive(false);
        playerListPanel.SetActive(true);

        roomBackButton.gameObject.SetActive(true);

        startGameButton.gameObject.SetActive(
            isHost
        );

        // -----------------------------------------------------
        // SHOW IP
        // -----------------------------------------------------

        if (copyIPButtonText != null &&
            networkSessionManager != null)
        {
            string ip;

            if (isHost)
            {
                ip =
                    networkSessionManager
                    .GetLocalIPAddress();
            }
            else
            {
                ip =
                    networkSessionManager
                    .GetHostAddress();
            }

            copyIPButtonText.text = ip;
        }

        RefreshPlayerList();

        Invoke(
            nameof(RefreshPlayerList),
            0.2f
        );
    }

    // =========================================================
    // CONNECTION PANEL BACK
    // =========================================================

    private void OnConnectionBackClicked()
    {
        CancelConnectionTimeout();

        isLeavingRoom = true;
        isConnecting = false;

        if (networkSessionManager != null)
        {
            networkSessionManager.Shutdown();
        }

        isLeavingRoom = false;

        SceneManager.LoadScene(
            mainMenuScene
        );
    }

    // =========================================================
    // ROOM BACK
    // =========================================================

    private void OnRoomBackClicked()
    {
        CancelConnectionTimeout();

        isLeavingRoom = true;
        isConnecting = false;

        CancelInvoke(
            nameof(RefreshPlayerList)
        );

        if (networkSessionManager != null)
        {
            networkSessionManager.Shutdown();
        }

        ShowConnectionUI();

        isLeavingRoom = false;
    }

    // =========================================================
    // SHOW CONNECTION UI
    // =========================================================

    private void ShowConnectionUI()
    {
        connectionPanel.SetActive(true);
        playerListPanel.SetActive(false);

        roomBackButton.gameObject.SetActive(false);
        startGameButton.gameObject.SetActive(false);

        playerListText.text = "";

        SetConnectionButtonsInteractable(
            true
        );
    }

    // =========================================================
    // START GAME
    // =========================================================

    private void OnStartGameClicked()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsHost)
            return;

        if (!NetworkManager.Singleton.IsListening)
            return;

        NetworkManager.Singleton.SceneManager.LoadScene(
            gameScene,
            LoadSceneMode.Single
        );
    }

    // =========================================================
    // COPY IP
    // =========================================================

    private void OnCopyIPClicked()
    {
        if (networkSessionManager == null)
            return;

        string ip;

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsHost)
        {
            // Host copies its own Radmin IP.
            ip =
                networkSessionManager
                .GetLocalIPAddress();
        }
        else
        {
            // Client copies host IP.
            ip =
                networkSessionManager
                .GetHostAddress();
        }

        if (string.IsNullOrWhiteSpace(ip))
        {
            notificationManager.ShowNotification(
                "IP address unavailable!"
            );

            return;
        }

        GUIUtility.systemCopyBuffer = ip;

        notificationManager.ShowNotification(
            "IP copied!"
        );

        Debug.Log(
            $"LobbyManager: IP copied = {ip}"
        );
    }

    // =========================================================
    // PLAYER LIST
    // =========================================================

    public void RefreshPlayerListFromHeartbeat()
    {
        RefreshPlayerList();
    }

    private void RefreshPlayerList()
    {
        if (playerListText == null)
            return;

        playerListText.text = "";

        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsListening)
            return;

        LobbyPlayer[] players =
            FindObjectsByType<LobbyPlayer>(
                FindObjectsSortMode.None
            );

        List<LobbyPlayer> sortedPlayers =
            new List<LobbyPlayer>(players);

        sortedPlayers.Sort(
            (a, b) =>
                a.OwnerClientId.CompareTo(
                    b.OwnerClientId
                )
        );

        int number = 1;

        foreach (
            LobbyPlayer lobbyPlayer
            in sortedPlayers)
        {
            if (lobbyPlayer == null)
                continue;

            string playerName =
                lobbyPlayer.PlayerName.Value
                .ToString();

            if (string.IsNullOrWhiteSpace(
                playerName))
            {
                playerName = "Player";
            }

            playerListText.text +=
                number +
                ". " +
                playerName +
                "\n";

            number++;
        }
    }

    // =========================================================
    // UI HELPERS
    // =========================================================

    private void SetConnectionButtonsInteractable(
        bool interactable)
    {
        if (hostButton != null)
            hostButton.interactable =
                interactable;

        if (joinButton != null)
            joinButton.interactable =
                interactable;

        if (ipInputField != null)
            ipInputField.interactable =
                interactable;
    }

    // =========================================================
    // TIMEOUT HELPER
    // =========================================================

    private void CancelConnectionTimeout()
    {
        if (connectionTimeoutCoroutine != null)
        {
            StopCoroutine(
                connectionTimeoutCoroutine
            );

            connectionTimeoutCoroutine = null;
        }
    }
}