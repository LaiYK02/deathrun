using UnityEngine;
using TMPro;

public class PlayerSettings : MonoBehaviour
{
    public static string PlayerName
    {
        get
        {
            return PlayerPrefs.GetString(
                "PlayerName",
                "Player"
            );
        }

        set
        {
            PlayerPrefs.SetString(
                "PlayerName",
                value
            );

            PlayerPrefs.Save();
        }
    }

    [Header("Main Menu")]
    [SerializeField] private TMP_InputField playerNameInputField;

    private void Start()
    {
        // Load the saved player name into the input field.
        if (playerNameInputField != null)
        {
            playerNameInputField.text = PlayerName;
        }
    }

    // Called by TMP_InputField -> On Value Changed
    public void SetPlayerName(string value)
    {
        value = value.Trim();

        if (string.IsNullOrEmpty(value))
        {
            value = "Player";
        }

        PlayerName = value;
    }
}