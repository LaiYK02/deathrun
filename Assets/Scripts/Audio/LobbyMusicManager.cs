using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LobbyMusicManager : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioClip normalMusic;
    [SerializeField] private AudioClip funnyMusic;

    private AudioSource audioSource;

    private GameSettingsManager settingsManager;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        audioSource =
            GetComponent<AudioSource>();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        settingsManager =
            GameSettingsManager.Instance;

        if (settingsManager == null)
        {
            Debug.LogError(
                "LobbyMusicManager: " +
                "GameSettingsManager was not found."
            );

            return;
        }

        settingsManager.OnSettingsChanged +=
            UpdateMusic;

        UpdateMusic();
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (settingsManager != null)
        {
            settingsManager.OnSettingsChanged -=
                UpdateMusic;
        }
    }

    // =========================================================
    // UPDATE MUSIC
    // =========================================================

    private void UpdateMusic()
    {
        if (audioSource == null)
            return;

        if (settingsManager == null)
            return;

        AudioClip targetClip;

        if (settingsManager.VeryFunnyMode)
        {
            targetClip = funnyMusic;
        }
        else
        {
            targetClip = normalMusic;
        }

        if (targetClip == null)
        {
            Debug.LogWarning(
                "LobbyMusicManager: " +
                "Target music clip is not assigned."
            );

            return;
        }

        // Do not restart the music if the selected
        // clip is already playing.
        if (audioSource.clip == targetClip &&
            audioSource.isPlaying)
        {
            return;
        }

        audioSource.clip = targetClip;

        audioSource.Play();
    }
}