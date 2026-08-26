using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkSessionManager : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private string hostAddress = "127.0.0.1";
    [SerializeField] private ushort port = 7777;

    private UnityTransport transport;

    private void Awake()
    {
        transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport == null)
        {
            Debug.LogError(
                "NetworkSessionManager: UnityTransport not found."
            );
        }
    }

    public void SetHostAddress(string address)
    {
        hostAddress = address;
    }

    public void StartHost()
    {
        if (transport == null)
            return;

        // Listen on all local interfaces.
        transport.SetConnectionData(
            "0.0.0.0",
            port,
            "0.0.0.0"
        );

        if (!NetworkManager.Singleton.StartHost())
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
        if (transport == null)
            return;

        transport.SetConnectionData(
            hostAddress,
            port
        );

        if (!NetworkManager.Singleton.StartClient())
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
        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}