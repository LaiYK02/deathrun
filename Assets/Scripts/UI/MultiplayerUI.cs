using UnityEngine;
using TMPro;
using Unity.Netcode;

public class MultiplayerUI : MonoBehaviour
{
    [Header("UI References")]
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
    }

    public void HostGame()
    {
        if (networkSessionManager == null)
        {
            SetStatus("Network manager not assigned.");
            return;
        }

        SetStatus("Starting host...");

        networkSessionManager.StartHost();
    }

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

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = "Status: " + message;
        }
    }
}