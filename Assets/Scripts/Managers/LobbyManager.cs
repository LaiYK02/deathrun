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

    [Header("Room UI Text")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playerListText;

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
        // GET PERSISTENT NETWORK SESSION MANAGER
        // -----------------------------------------------------

        networkSessionManager =
            NetworkSessionManager.Instance;

        if (networkSessionManager == null)
        {
            Debug.LogError(
                "LobbyManager: NetworkSessionManager was not found."
            );

            statusText.text =
                "Status: Network manager not found";

            return;
        }

        // -----------------------------------------------------
        // BUTTON LISTENERS
        // -----------------------------------------------------

        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
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

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback +=
                OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback +=
                OnClientDisconnected;
        }

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
        if (connectionTimeoutCoroutine != null)
        {
            StopCoroutine(connectionTimeoutCoroutine);
            connectionTimeoutCoroutine = null;
        }

        if (hostButton != null)
            hostButton.onClick.RemoveListener(OnHostClicked);

        if (joinButton != null)
            joinButton.onClick.RemoveListener(OnJoinClicked);

        if (connectionBackButton != null)
            connectionBackButton.onClick.RemoveListener(
                OnConnectionBackClicked
            );

        if (roomBackButton != null)
            roomBackButton.onClick.RemoveListener(
                OnRoomBackClicked
            );

        if (startGameButton != null)
            startGameButton.onClick.RemoveListener(
                OnStartGameClicked
            );

        if (copyIPButton != null)
            copyIPButton.onClick.RemoveListener(
                OnCopyIPClicked
            );

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
            Debug.LogError(
                "LobbyManager: NetworkSessionManager not found."
            );

            return;
        }

        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.IsListening)
            return;

        isConnecting = true;
        isLeavingRoom = false;

        SetConnectionButtonsInteractable(false);

        statusText.text =
            "Status: Starting host...";

        bool success =
            networkSessionManager.StartHost();

        if (!success)
        {
            isConnecting = false;

            SetConnectionButtonsInteractable(true);

            statusText.text =
                "Status: Failed to host";

            return;
        }

        // The local host client will trigger
        // OnClientConnected().
    }

    // =========================================================
    // JOIN
    // =========================================================

    private void OnJoinClicked()
    {
        if (networkSessionManager == null)
        {
            Debug.LogError(
                "LobbyManager: NetworkSessionManager not found."
            );

            return;
        }

        if (NetworkManager.Singleton == null)
            return;

        string ip =
            ipInputField.text.Trim();

        if (string.IsNullOrEmpty(ip))
        {
            statusText.text =
                "Status: Enter an IP address";

            return;
        }

        if (NetworkManager.Singleton.IsListening)
            return;

        networkSessionManager.SetHostAddress(ip);

        isConnecting = true;
        isLeavingRoom = false;

        SetConnectionButtonsInteractable(false);

        statusText.text =
            "Status: Connecting...";

        bool success =
            networkSessionManager.StartClient();

        if (!success)
        {
            isConnecting = false;

            SetConnectionButtonsInteractable(true);

            statusText.text =
                "Status: Failed to connect";

            return;
        }

        // Start timeout.
        if (connectionTimeoutCoroutine != null)
        {
            StopCoroutine(connectionTimeoutCoroutine);
        }

        connectionTimeoutCoroutine =
            StartCoroutine(ConnectionTimeout());
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

        statusText.text =
            "Status: Connection timed out";
    }

    // =========================================================
    // CLIENT CONNECTED
    // =========================================================

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log(
            $"LobbyManager: Client connected. " +
            $"Client ID = {clientId}"
        );

        if (NetworkManager.Singleton == null)
            return;

        // -----------------------------------------------------
        // CANCEL CONNECTION TIMEOUT
        // -----------------------------------------------------

        if (connectionTimeoutCoroutine != null)
        {
            StopCoroutine(connectionTimeoutCoroutine);
            connectionTimeoutCoroutine = null;
        }

        // -----------------------------------------------------
        // HOST
        // -----------------------------------------------------

        if (NetworkManager.Singleton.IsHost)
        {
            isConnecting = false;

            EnterRoom(true);

            Debug.Log(
                "LobbyManager: Host entered room."
            );

            return;
        }

        // -----------------------------------------------------
        // CLIENT
        // -----------------------------------------------------

        if (clientId ==
            NetworkManager.Singleton.LocalClientId)
        {
            isConnecting = false;

            EnterRoom(false);

            Debug.Log(
                "LobbyManager: Successfully joined host."
            );
        }
    }

    // =========================================================
    // CLIENT DISCONNECTED
    // =========================================================

    private void OnClientDisconnected(ulong clientId)
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
        //
        // Client failed to join or host disappeared.
        // -----------------------------------------------------

        if (!NetworkManager.Singleton.IsHost)
        {
            if (clientId ==
                NetworkManager.Singleton.LocalClientId)
            {
                isConnecting = false;

                Debug.Log(
                    "LobbyManager: Connection failed " +
                    "or host disconnected."
                );

                ShowConnectionUI();

                statusText.text =
                    "Status: Connection failed";

                SetConnectionButtonsInteractable(true);
            }

            return;
        }

        // -----------------------------------------------------
        // HOST
        //
        // Another player disconnected.
        // -----------------------------------------------------

        RefreshPlayerList();

        statusText.text =
            "Status: Hosting";

        Debug.Log(
            $"LobbyManager: Player {clientId} " +
            "left the room."
        );
    }

    // =========================================================
    // ENTER ROOM
    // =========================================================

    private void EnterRoom(bool isHost)
    {
        connectionPanel.SetActive(false);
        playerListPanel.SetActive(true);

        roomBackButton.gameObject.SetActive(true);

        // Only host sees START GAME.
        startGameButton.gameObject.SetActive(isHost);

        if (isHost)
        {
            statusText.text =
                "Status: Hosting";
        }
        else
        {
            statusText.text =
                "Status: Connected";
        }

        RefreshPlayerList();

        // Give NetworkObjects a moment to spawn.
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

        // If somehow still connecting,
        // cancel the network session.
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

        Debug.Log(
            "LobbyManager: Leaving room."
        );

        if (networkSessionManager != null)
        {
            networkSessionManager.Shutdown();
        }

        // Return to the connection panel,
        // but remain in Lobby.
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

        statusText.text =
            "Status: Disconnected";

        playerListText.text = "";

        SetConnectionButtonsInteractable(true);
    }

    // =========================================================
    // START GAME
    // =========================================================

    private void OnStartGameClicked()
    {
        if (NetworkManager.Singleton == null)
            return;

        // Only host can start.
        if (!NetworkManager.Singleton.IsHost)
            return;

        if (!NetworkManager.Singleton.IsListening)
            return;

        Debug.Log(
            "LobbyManager: Host starting game."
        );

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

        string ip =
            networkSessionManager.GetLocalIPAddress();

        GUIUtility.systemCopyBuffer = ip;

        statusText.text =
            "Status: IP copied";

        Debug.Log(
            $"LobbyManager: IP copied = {ip}"
        );
    }

    // =========================================================
    // PLAYER LIST
    // =========================================================

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

        foreach (LobbyPlayer lobbyPlayer in sortedPlayers)
        {
            if (lobbyPlayer == null)
                continue;

            string playerName =
                lobbyPlayer.PlayerName.Value.ToString();

            if (string.IsNullOrWhiteSpace(playerName))
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
        bool interactable
    )
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