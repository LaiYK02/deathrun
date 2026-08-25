using UnityEngine;
using TMPro;

public class SpeedDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private TMP_Text speedText;

    [Header("Display")]
    [SerializeField] private string prefix = "Speed: ";
    [SerializeField] private int decimalPlaces = 0;

    private void Update()
    {
        if (playerMovement == null || speedText == null)
            return;

        float speed = playerMovement.CurrentSpeed;

        speedText.text = prefix + speed.ToString($"F{decimalPlaces}");
    }
}