using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    private const string MasterVolumeParameter =
        "MasterVolume";

    private const string MusicVolumeParameter =
        "MusicVolume";

    private const string SFXVolumeParameter =
        "SFXVolume";

    private void Start()
    {
        ApplyAllVolumes();

        if (GameSettingsManager.Instance != null)
        {
            GameSettingsManager.Instance.OnSettingsChanged +=
                ApplyAllVolumes;
        }
    }

    private void OnDestroy()
    {
        if (GameSettingsManager.Instance != null)
        {
            GameSettingsManager.Instance.OnSettingsChanged -=
                ApplyAllVolumes;
        }
    }

    private void ApplyAllVolumes()
    {
        if (audioMixer == null)
            return;

        GameSettingsManager settings =
            GameSettingsManager.Instance;

        if (settings == null)
            return;

        SetMixerVolume(
            MasterVolumeParameter,
            settings.MasterVolume
        );

        SetMixerVolume(
            MusicVolumeParameter,
            settings.MusicVolume
        );

        SetMixerVolume(
            SFXVolumeParameter,
            settings.SFXVolume
        );
    }

    private void SetMixerVolume(
        string parameter,
        float normalizedValue)
    {
        // Convert 0-1 into decibels.
        // 0 volume = -80 dB.
        // 1 volume = 0 dB.

        float decibels;

        if (normalizedValue <= 0.001f)
        {
            decibels = -80f;
        }
        else
        {
            decibels =
                Mathf.Log10(normalizedValue) * 20f;
        }

        audioMixer.SetFloat(
            parameter,
            decibels
        );
    }
}