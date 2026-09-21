using TMPro;
using UnityEngine;

public class RoundTimerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text timerText;

    private void Update()
    {
        if (RoundManager.Instance == null)
        {
            if (timerText != null)
                timerText.text = "00:00";

            return;
        }

        int timeRemaining =
            RoundManager.Instance.PhaseTimeRemaining.Value;

        timeRemaining =
            Mathf.Max(
                0,
                timeRemaining
            );

        int minutes =
            timeRemaining / 60;

        int seconds =
            timeRemaining % 60;

        if (timerText != null)
        {
            timerText.text =
                $"{minutes:00}:{seconds:00}";
        }
    }
}