using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance { get; private set; }

    [Header("Normal UI Sounds")]
    [SerializeField] private AudioClip normalHoverSound;
    [SerializeField] private AudioClip normalClickSound;

    [Header("Normal Mode Volume")]
    [SerializeField, Range(0f, 1f)]
    private float normalHoverVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float normalClickVolume = 1f;

    [Header("Very Funny Mode UI Sounds")]
    [SerializeField] private AudioClip funnyHoverSound;
    [SerializeField] private AudioClip funnyClickSound;

    [Header("Very Funny Mode Volume")]
    [SerializeField, Range(0f, 1f)]
    private float funnyHoverVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float funnyClickVolume = 1f;

    private AudioSource audioSource;
    private GameSettingsManager settingsManager;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        settingsManager =
            GameSettingsManager.Instance;
    }

    public void PlayHoverSound()
    {
        if (audioSource == null)
            return;

        AudioClip soundToPlay =
            GetHoverSound();

        if (soundToPlay == null)
            return;

        float volume =
            GetHoverVolume();

        audioSource.PlayOneShot(
            soundToPlay,
            volume
        );
    }

    public void PlayClickSound()
    {
        if (audioSource == null)
            return;

        AudioClip soundToPlay =
            GetClickSound();

        if (soundToPlay == null)
            return;

        float volume =
            GetClickVolume();

        audioSource.PlayOneShot(
            soundToPlay,
            volume
        );
    }

    private bool IsVeryFunnyMode()
    {
        if (settingsManager == null)
        {
            settingsManager =
                GameSettingsManager.Instance;
        }

        return settingsManager != null &&
               settingsManager.VeryFunnyMode;
    }

    private AudioClip GetHoverSound()
    {
        if (IsVeryFunnyMode())
        {
            return funnyHoverSound;
        }

        return normalHoverSound;
    }

    private AudioClip GetClickSound()
    {
        if (IsVeryFunnyMode())
        {
            return funnyClickSound;
        }

        return normalClickSound;
    }

    private float GetHoverVolume()
    {
        if (IsVeryFunnyMode())
        {
            return funnyHoverVolume;
        }

        return normalHoverVolume;
    }

    private float GetClickVolume()
    {
        if (IsVeryFunnyMode())
        {
            return funnyClickVolume;
        }

        return normalClickVolume;
    }
}
