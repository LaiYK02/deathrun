using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net.Sockets;
using System.Net;

[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UnityTransport))]
public class NetworkSessionManager : MonoBehaviour
{
    public static NetworkSessionManager Instance { get; private set; }

    [Header("Connection")]
    [SerializeField] private string hostAddress = "127.0.0.1";
    [SerializeField] private ushort port = 7777;

    private NetworkManager networkManager;
    private UnityTransport transport;

    private void Awake()
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

        // Keep this object when changing scenes.
        DontDestroyOnLoad(gameObject);

        // -----------------------------------------------------
        // COMPONENTS
        // -----------------------------------------------------

        networkManager = GetComponent<NetworkManager>();
        transport = GetComponent<UnityTransport>();

        if (networkManager == null)
        {
            Debug.LogError(
                "NetworkSessionManager: NetworkManager component not found."
            );
        }

        if (transport == null)
        {
            Debug.LogError(
                "NetworkSessionManager: UnityTransport component not found."
            );
        }

        networkManager.NetworkConfig.NetworkTransport = transport;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // =========================================================
    // HOST ADDRESS
    // =========================================================

    public void SetHostAddress(string address)
    {
        hostAddress = address.Trim();
    }

    public string GetHostAddress()
    {
        return hostAddress;
    }

    // =========================================================
    // START HOST
    // =========================================================

    public bool StartHost()
    {
        if (networkManager == null || transport == null)
            return false;

        if (networkManager.IsListening)
        {
            Debug.LogWarning(
                "NetworkSessionManager: Network is already running."
            );

            return false;
        }

        // Listen on all local network interfaces.
        transport.SetConnectionData(
            "0.0.0.0",
            port,
            "0.0.0.0"
        );

        bool success = networkManager.StartHost();

        if (!success)
        {
            Debug.LogError(
                "NetworkSessionManager: Failed to start Host."
            );

            return false;
        }

        Debug.Log(
            $"NetworkSessionManager: Host started on port {port}."
        );

        return true;
    }

    // =========================================================
    // START CLIENT
    // =========================================================

    public bool StartClient()
    {
        if (networkManager == null || transport == null)
            return false;

        if (networkManager.IsListening)
        {
            Debug.LogWarning(
                "NetworkSessionManager: Network is already running."
            );

            return false;
        }

        transport.SetConnectionData(
            hostAddress,
            port
        );

        bool success = networkManager.StartClient();

        if (!success)
        {
            Debug.LogError(
                $"NetworkSessionManager: Failed to start client for " +
                $"{hostAddress}:{port}"
            );

            return false;
        }

        Debug.Log(
            $"NetworkSessionManager: Connecting to " +
            $"{hostAddress}:{port}"
        );

        return true;
    }

    // =========================================================
    // SHUTDOWN
    // =========================================================

    public void Shutdown()
    {
        if (networkManager == null)
            return;

        if (!networkManager.IsListening)
            return;

        Debug.Log(
            "NetworkSessionManager: Shutting down network."
        );

        networkManager.Shutdown();
    }

    // =========================================================
    // LOCAL IP
    // =========================================================

    public string GetLocalIPAddress()
    {
        string localIP = "127.0.0.1";

        try
        {
            IPHostEntry host =
                Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                string address = ip.ToString();

                // Prefer Radmin VPN address.
                if (address.StartsWith("26."))
                {
                    return address;
                }

                localIP = address;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Could not find local IP address: " +
                e.Message
            );
        }

        return localIP;
    }
}