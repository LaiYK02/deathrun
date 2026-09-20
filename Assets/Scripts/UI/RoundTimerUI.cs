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
                timerText.text = "";

            return;
        }

        RoundPhase phase =
            RoundManager.Instance.Phase.Value;

        int timeRemaining =
            RoundManager.Instance.PhaseTimeRemaining.Value;

        switch (phase)
        {
            case RoundPhase.Warmup:

                timerText.text = timeRemaining.ToString();

                break;

            case RoundPhase.Active:

                timerText.text =
                    "ROUND " +
                    RoundManager.Instance.CurrentRound.Value;

                break;

            case RoundPhase.Ending:

                timerText.text = timeRemaining.ToString();

                break;
        }
    }
}