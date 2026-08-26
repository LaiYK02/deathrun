using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UnityTransport))]
public class NetworkSessionManager : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private string hostAddress = "127.0.0.1";
    [SerializeField] private ushort port = 7777;

    private NetworkManager networkManager;
    private UnityTransport transport;

    private void Awake()
    {
        // Get the components directly from this GameObject.
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
    }

    public void SetHostAddress(string address)
    {
        hostAddress = address;
    }

    public void StartHost()
    {
        if (networkManager == null || transport == null)
            return;

        // Listen on all local network interfaces.
        transport.SetConnectionData(
            "0.0.0.0",
            port,
            "0.0.0.0"
        );

        if (!networkManager.StartHost())
        {
            Debug.LogError("Failed to start Host.");
        }
        else
        {
            Debug.Log(
                $"Host started on port {port}."
            );
        }
    }

    public void StartClient()
    {
        if (networkManager == null || transport == null)
            return;

        transport.SetConnectionData(
            hostAddress,
            port
        );

        if (!networkManager.StartClient())
        {
            Debug.LogError(
                $"Failed to connect to {hostAddress}:{port}"
            );
        }
        else
        {
            Debug.Log(
                $"Connecting to {hostAddress}:{port}"
            );
        }
    }

    public void Shutdown()
    {
        if (networkManager != null &&
            networkManager.IsListening)
        {
            networkManager.Shutdown();
        }
    }
}