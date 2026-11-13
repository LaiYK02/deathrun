using TMPro;
using UnityEngine;

public class RoundScoreUI : MonoBehaviour
{
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private TMP_Text runnersScoreText;
    [SerializeField] private TMP_Text trapperScoreText;

    private int displayedRunnersScore = -1;
    private int displayedTrapperScore = -1;

    private void Update()
    {
        int runnersScore = roundManager.RunnersScore.Value;
        if (displayedRunnersScore != runnersScore)
        {
            displayedRunnersScore = runnersScore;
            runnersScoreText.text = runnersScore.ToString();
        }

        int trapperScore = roundManager.TrapperScore.Value;
        if (displayedTrapperScore != trapperScore)
        {
            displayedTrapperScore = trapperScore;
            trapperScoreText.text = trapperScore.ToString();
        }
    }
}
