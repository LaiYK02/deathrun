using UnityEngine;

public class MainMenuPlayerPreview : MonoBehaviour
{
    [SerializeField] private Animator playerAnimator;

    private void Start()
    {
        PlayRandomDance();
    }

    private void PlayRandomDance()
    {
        // Random number from 1 to 5
        int danceNumber = Random.Range(1, 6);

        string danceState = "Dance" + danceNumber;

        // Play the selected dance animation
        playerAnimator.Play(danceState, 0, 0f);

        Debug.Log("Main Menu Player selected: " + danceState);
    }
}