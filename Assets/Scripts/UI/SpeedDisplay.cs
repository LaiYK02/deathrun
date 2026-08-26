using UnityEngine;
using Unity.Netcode;
using TMPro;

public class SpeedDisplay : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private TMP_Text speedText;

    [Header("Display")]
    [SerializeField] private string prefix = "Speed: ";
    [SerializeField] private int decimalPlaces = 0;

    private void Update()
    {
        if (!IsOwner)
            return;

        if (playerMovement == null || speedText == null)
            return;

        float speed = playerMovement.CurrentSpeed;

        speedText.text =
            prefix + speed.ToString($"F{decimalPlaces}");
    }
}