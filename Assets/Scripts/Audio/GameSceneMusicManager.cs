using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameSceneMusicManager : MonoBehaviour
{
    public static GameSceneMusicManager Instance { get; private set; }

    // =========================================================
    // GAMEPLAY MUSIC
    // =========================================================

    [Header("Gameplay BGM - Normal Mode")]
    [SerializeField] private AudioClip[] normalGameplayMusic;

    [Header("Gameplay BGM - Funny Mode")]
    [SerializeField] private AudioClip[] funnyGameplayMusic;

    [Header("Gameplay BGM Settings")]
    [SerializeField, Range(0f, 1f)]
    private float gameplayMusicVolume = 1f;

    [SerializeField]
    private float gameplayMusicStartDelay = 5f;

    // =========================================================
    // END ROUND MUSIC
    // =========================================================

    [Header("End Round BGM - Trapper Wins")]
    [SerializeField] private AudioClip trapperWinNormalMusic;

    [SerializeField] private AudioClip trapperWinFunnyMusic;

    [Header("End Round BGM - Runners Win")]
    [SerializeField] private AudioClip runnersWinNormalMusic;

    [SerializeField] private AudioClip runnersWinFunnyMusic;

    [Header("End Round BGM Settings")]
    [SerializeField, Range(0f, 1f)]
    private float endRoundMusicVolume = 1f;

    // =========================================================
    // AUDIO SOURCES
    // =========================================================

    [Header("Audio Sources")]
    [SerializeField] private AudioSource gameplayAudioSource;
    [SerializeField] private AudioSource endRoundAudioSource;

    // =========================================================
    // REFERENCES
    // =========================================================

    private GameSettingsManager settingsManager;

    // =========================================================
    // GAMEPLAY PLAYLIST STATE
    // =========================================================

    private List<AudioClip> remainingGameplayTracks =
        new List<AudioClip>();

    private RoundPhase previousPhase =
        RoundPhase.Warmup;

    private int previousRound = 1;

    private bool gameplayMusicStarted;

    private Coroutine gameplayStartCoroutine;

    private bool gameFlowInitialized;

    // =========================================================
    // END ROUND STATE
    // =========================================================

    private bool endRoundMusicPlaying;

    private bool lastEndRoundWasTrapperWin;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ConfigureAudioSources();
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
                "GameSceneMusicManager: " +
                "GameSettingsManager was not found."
            );

            return;
        }

        settingsManager.OnSettingsChanged +=
            OnSettingsChanged;

        previousPhase =
            RoundManager.Instance != null
                ? RoundManager.Instance.Phase.Value
                : RoundPhase.Warmup;

        previousRound =
            RoundManager.Instance != null
                ? RoundManager.Instance.CurrentRound.Value
                : 1;

        ClearAllMusic();

        // Start checking the round state.
        gameFlowInitialized = true;

        Debug.Log(
            "GameSceneMusicManager: Music system initialized."
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!gameFlowInitialized)
            return;

        if (RoundManager.Instance == null)
            return;

        RoundPhase currentPhase =
            RoundManager.Instance.Phase.Value;

        int currentRound =
            RoundManager.Instance.CurrentRound.Value;

        // -----------------------------------------------------
        // DETECT PHASE CHANGE
        // -----------------------------------------------------

        if (currentPhase != previousPhase)
        {
            HandlePhaseChanged(
                previousPhase,
                currentPhase
            );

            previousPhase =
                currentPhase;
        }

        // -----------------------------------------------------
        // DETECT NEW ROUND
        // -----------------------------------------------------

        if (currentRound != previousRound)
        {
            HandleNewRound(
                previousRound,
                currentRound
            );

            previousRound =
                currentRound;
        }

        // -----------------------------------------------------
        // PLAY NEXT GAMEPLAY TRACK
        // -----------------------------------------------------

        if (gameplayMusicStarted &&
            currentPhase == RoundPhase.Active &&
            gameplayAudioSource != null &&
            !gameplayAudioSource.isPlaying)
        {
            PlayNextGameplayTrack();
        }
    }

    // =========================================================
    // PHASE CHANGED
    // =========================================================

    private void HandlePhaseChanged(
        RoundPhase oldPhase,
        RoundPhase newPhase)
    {
        Debug.Log(
            $"GameSceneMusicManager: " +
            $"Phase changed from {oldPhase} " +
            $"to {newPhase}."
        );

        // -----------------------------------------------------
        // WARMUP
        // -----------------------------------------------------

        if (newPhase == RoundPhase.Warmup)
        {
            StopGameplayMusic();
            StopEndRoundMusic();

            gameplayMusicStarted = false;

            CancelGameplayStartCoroutine();

            return;
        }

        // -----------------------------------------------------
        // ACTIVE ROUND
        // -----------------------------------------------------

        if (newPhase == RoundPhase.Active)
        {
            ScheduleGameplayMusicStart();

            return;
        }

        // -----------------------------------------------------
        // ROUND ENDING
        // -----------------------------------------------------

        if (newPhase == RoundPhase.Ending)
        {
            StartEndRoundMusic();

            return;
        }
    }

    // =========================================================
    // NEW ROUND
    // =========================================================

    private void HandleNewRound(
        int oldRound,
        int newRound)
    {
        Debug.Log(
            $"GameSceneMusicManager: " +
            $"New round detected: {newRound}."
        );

        ResetGameplayPlaylist();
    }

    // =========================================================
    // SCHEDULE GAMEPLAY MUSIC
    // =========================================================

    private void ScheduleGameplayMusicStart()
    {
        CancelGameplayStartCoroutine();

        gameplayMusicStarted = false;

        gameplayStartCoroutine =
            StartCoroutine(
                GameplayMusicStartCoroutine()
            );
    }

    // =========================================================
    // GAMEPLAY MUSIC DELAY
    // =========================================================

    private IEnumerator GameplayMusicStartCoroutine()
    {
        yield return new WaitForSeconds(
            gameplayMusicStartDelay
        );

        gameplayStartCoroutine = null;

        if (RoundManager.Instance == null)
            yield break;

        if (RoundManager.Instance.Phase.Value !=
            RoundPhase.Active)
        {
            yield break;
        }

        // -----------------------------------------------------
        // STOP END ROUND MUSIC
        // -----------------------------------------------------

        StopEndRoundMusic();

        // -----------------------------------------------------
        // START GAMEPLAY MUSIC
        // -----------------------------------------------------

        gameplayMusicStarted = true;

        if (gameplayAudioSource != null)
        {
            gameplayAudioSource.loop = false;

            if (!gameplayAudioSource.isPlaying)
            {
                PlayNextGameplayTrack();
            }
        }

        Debug.Log(
            "GameSceneMusicManager: " +
            "Gameplay BGM started."
        );
    }

    // =========================================================
    // START END ROUND MUSIC
    // =========================================================

    private void StartEndRoundMusic()
    {
        if (RoundManager.Instance == null)
            return;

        bool trapperWon =
            DetermineTrapperWin();

        lastEndRoundWasTrapperWin =
            trapperWon;

        AudioClip selectedClip =
            GetEndRoundClip(
                trapperWon
            );

        if (selectedClip == null)
        {
            Debug.LogWarning(
                "GameSceneMusicManager: " +
                "No End Round BGM is assigned for this result."
            );

            return;
        }

        CancelGameplayStartCoroutine();

        gameplayMusicStarted = false;

        StopGameplayMusic();

        if (endRoundAudioSource == null)
            return;

        endRoundAudioSource.clip = selectedClip;

        endRoundAudioSource.volume = GetEndRoundVolume();

        endRoundAudioSource.loop = false;

        endRoundAudioSource.Play();

        endRoundMusicPlaying = true;

        Debug.Log(
            $"GameSceneMusicManager: " +
            $"End Round BGM started. Winner = " +
            $"{(trapperWon ? "Trapper" : "Runners")}."
        );
    }

    // =========================================================
    // DETERMINE WINNER
    // =========================================================

    private bool DetermineTrapperWin()
    {
        if (RoundManager.Instance == null)
            return false;

        return RoundManager.Instance
            .CurrentRoundEndResult.Value ==
            RoundEndResult.TrapperWin;
    }

    // =========================================================
    // GET END ROUND CLIP
    // =========================================================

    private AudioClip GetEndRoundClip(
        bool trapperWon)
    {
        bool funnyMode =
            settingsManager != null &&
            settingsManager.VeryFunnyMode;

        if (trapperWon)
        {
            return funnyMode
                ? trapperWinFunnyMusic
                : trapperWinNormalMusic;
        }

        return funnyMode
            ? runnersWinFunnyMusic
            : runnersWinNormalMusic;
    }

    // =========================================================
    // GAMEPLAY PLAYLIST
    // =========================================================

    private void ResetGameplayPlaylist()
    {
        remainingGameplayTracks.Clear();

        AudioClip[] source =
            GetCurrentGameplayPlaylist();

        if (source == null)
            return;

        foreach (AudioClip clip in source)
        {
            if (clip != null)
            {
                remainingGameplayTracks.Add(
                    clip
                );
            }
        }

        ShuffleList(
            remainingGameplayTracks
        );
    }

    // =========================================================
    // PLAY NEXT GAMEPLAY TRACK
    // =========================================================

    private void PlayNextGameplayTrack()
    {
        if (gameplayAudioSource == null)
            return;

        if (remainingGameplayTracks.Count == 0)
        {
            ResetGameplayPlaylist();
        }

        if (remainingGameplayTracks.Count == 0)
        {
            Debug.LogWarning(
                "GameSceneMusicManager: " +
                "No gameplay BGM clips are assigned."
            );

            return;
        }

        int lastIndex =
            remainingGameplayTracks.Count - 1;

        AudioClip nextClip =
            remainingGameplayTracks[lastIndex];

        remainingGameplayTracks.RemoveAt(
            lastIndex
        );

        if (nextClip == null)
        {
            PlayNextGameplayTrack();
            return;
        }

        gameplayAudioSource.clip =
            nextClip;

        gameplayAudioSource.volume =
            GetGameplayVolume();

        gameplayAudioSource.loop = false;

        gameplayAudioSource.Play();

        Debug.Log(
            $"GameSceneMusicManager: " +
            $"Playing gameplay BGM: {nextClip.name}"
        );
    }

    // =========================================================
    // GET CURRENT GAMEPLAY PLAYLIST
    // =========================================================

    private AudioClip[] GetCurrentGameplayPlaylist()
    {
        bool funnyMode =
            settingsManager != null &&
            settingsManager.VeryFunnyMode;

        return funnyMode
            ? funnyGameplayMusic
            : normalGameplayMusic;
    }

    // =========================================================
    // SHUFFLE
    // =========================================================

    private void ShuffleList(
        List<AudioClip> list)
    {
        for (int i = list.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            AudioClip temp =
                list[i];

            list[i] =
                list[randomIndex];

            list[randomIndex] =
                temp;
        }
    }

    // =========================================================
    // SETTINGS CHANGED
    // =========================================================

    private void OnSettingsChanged()
    {
        if (settingsManager == null)
            return;

        // Update volumes immediately.
        UpdateVolumes();

        if (gameplayMusicStarted &&
            RoundManager.Instance != null &&
            RoundManager.Instance.Phase.Value ==
                RoundPhase.Active)
        {
            ResetGameplayPlaylist();

            if (gameplayAudioSource != null)
            {
                gameplayAudioSource.Stop();
            }

            PlayNextGameplayTrack();
        }

        if (endRoundMusicPlaying &&
            RoundManager.Instance != null &&
            RoundManager.Instance.Phase.Value ==
                RoundPhase.Ending)
        {
            StartEndRoundMusic();
        }
    }

    // =========================================================
    // UPDATE VOLUMES
    // =========================================================

    private void UpdateVolumes()
    {
        if (gameplayAudioSource != null)
        {
            gameplayAudioSource.volume =
                GetGameplayVolume();
        }

        if (endRoundAudioSource != null)
        {
            endRoundAudioSource.volume =
                GetEndRoundVolume();
        }
    }

    // =========================================================
    // GAMEPLAY VOLUME
    // =========================================================

    private float GetGameplayVolume()
    {
        float settingsVolume =
            settingsManager != null
                ? settingsManager.MusicVolume
                : 1f;

        return Mathf.Clamp01(
            gameplayMusicVolume *
            settingsVolume
        );
    }

    // =========================================================
    // END ROUND VOLUME
    // =========================================================

    private float GetEndRoundVolume()
    {
        float settingsVolume =
            settingsManager != null
                ? settingsManager.MusicVolume
                : 1f;

        return Mathf.Clamp01(
            endRoundMusicVolume *
            settingsVolume
        );
    }

    // =========================================================
    // STOP GAMEPLAY
    // =========================================================

    private void StopGameplayMusic()
    {
        if (gameplayAudioSource == null)
            return;

        gameplayAudioSource.Stop();
        gameplayAudioSource.clip = null;
    }

    // =========================================================
    // STOP END ROUND
    // =========================================================

    private void StopEndRoundMusic()
    {
        if (endRoundAudioSource == null)
            return;

        endRoundAudioSource.Stop();
        endRoundAudioSource.clip = null;

        endRoundMusicPlaying = false;
    }

    // =========================================================
    // CLEAR ALL MUSIC
    // =========================================================

    private void ClearAllMusic()
    {
        StopGameplayMusic();
        StopEndRoundMusic();

        gameplayMusicStarted = false;

        remainingGameplayTracks.Clear();
    }

    // =========================================================
    // CANCEL GAMEPLAY COROUTINE
    // =========================================================

    private void CancelGameplayStartCoroutine()
    {
        if (gameplayStartCoroutine != null)
        {
            StopCoroutine(
                gameplayStartCoroutine
            );

            gameplayStartCoroutine = null;
        }
    }

    // =========================================================
    // AUDIO SOURCE SETUP
    // =========================================================

    private void ConfigureAudioSources()
    {
        // The required AudioSource becomes the gameplay source
        // if one has not been manually assigned.

        AudioSource ownAudioSource =
            GetComponent<AudioSource>();

        if (gameplayAudioSource == null)
        {
            gameplayAudioSource =
                ownAudioSource;
        }

        ConfigureSource(
            gameplayAudioSource
        );

        ConfigureSource(
            endRoundAudioSource
        );
    }

    // =========================================================
    // CONFIGURE SOURCE
    // =========================================================

    private void ConfigureSource(
        AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 1f;
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (settingsManager != null)
        {
            settingsManager.OnSettingsChanged -=
                OnSettingsChanged;
        }

        CancelGameplayStartCoroutine();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}