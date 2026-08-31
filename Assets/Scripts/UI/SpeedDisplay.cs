using UnityEngine;
using Unity.Netcode;
using TMPro;

public class SpeedDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text speedText;

    [Header("Display")]
    [SerializeField] private string prefix = "Speed: ";
    [SerializeField] private int decimalPlaces = 2;

    private PlayerMovement playerMovement;

    private void Start()
    {
        FindLocalPlayer();
    }

    private void Update()
    {
        // Player may not have spawned yet.
        if (playerMovement == null)
        {
            FindLocalPlayer();
            return;
        }

        float speed = playerMovement.CurrentSpeed;

        speedText.text =
            prefix + speed.ToString($"F{decimalPlaces}");
    }

    private void FindLocalPlayer()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsClient)
            return;

        NetworkObject localPlayer =
            NetworkManager.Singleton.LocalClient?.PlayerObject;

        if (localPlayer == null)
            return;

        playerMovement =
            localPlayer.GetComponent<PlayerMovement>();
    }
}