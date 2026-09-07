using System;
using UnityEngine;

public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    // =========================================================
    // PLAYER PREF KEYS
    // =========================================================

    private const string HorizontalSensitivityKey =
        "Settings_HorizontalSensitivity";

    private const string VerticalSensitivityKey =
        "Settings_VerticalSensitivity";

    private const string CrosshairKey =
        "Settings_Crosshair";

    private const string StartingCameraKey =
        "Settings_StartingCamera";

    private const string MasterVolumeKey =
        "Settings_MasterVolume";

    private const string MusicVolumeKey =
        "Settings_MusicVolume";

    private const string SFXVolumeKey =
        "Settings_SFXVolume";

    // =========================================================
    // DEFAULT VALUES
    // =========================================================

    private const float DefaultHorizontalSensitivity = 0.2f;
    private const float DefaultVerticalSensitivity = 0.08f;

    private const bool DefaultCrosshairEnabled = true;

    // 0 = Third Person
    // 1 = First Person
    private const int DefaultStartingCamera = 0;

    private const float DefaultMasterVolume = 1f;
    private const float DefaultMusicVolume = 1f;
    private const float DefaultSFXVolume = 1f;

    // =========================================================
    // EVENTS
    // =========================================================

    public event Action OnSettingsChanged;

    // =========================================================
    // SETTINGS
    // =========================================================

    public float HorizontalSensitivity { get; private set; }

    public float VerticalSensitivity { get; private set; }

    public bool CrosshairEnabled { get; private set; }

    public int StartingCamera { get; private set; }

    public float MasterVolume { get; private set; }

    public float MusicVolume { get; private set; }

    public float SFXVolume { get; private set; }

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    // =========================================================
    // LOAD SETTINGS
    // =========================================================

    private void LoadSettings()
    {
        HorizontalSensitivity =
            PlayerPrefs.GetFloat(
                HorizontalSensitivityKey,
                DefaultHorizontalSensitivity
            );

        VerticalSensitivity =
            PlayerPrefs.GetFloat(
                VerticalSensitivityKey,
                DefaultVerticalSensitivity
            );

        CrosshairEnabled =
            PlayerPrefs.GetInt(
                CrosshairKey,
                DefaultCrosshairEnabled ? 1 : 0
            ) == 1;

        StartingCamera =
            PlayerPrefs.GetInt(
                StartingCameraKey,
                DefaultStartingCamera
            );

        MasterVolume =
            PlayerPrefs.GetFloat(
                MasterVolumeKey,
                DefaultMasterVolume
            );

        MusicVolume =
            PlayerPrefs.GetFloat(
                MusicVolumeKey,
                DefaultMusicVolume
            );

        SFXVolume =
            PlayerPrefs.GetFloat(
                SFXVolumeKey,
                DefaultSFXVolume
            );
    }

    // =========================================================
    // HORIZONTAL SENSITIVITY
    // =========================================================

    public void SetHorizontalSensitivity(float value)
    {
        HorizontalSensitivity = value;

        PlayerPrefs.SetFloat(
            HorizontalSensitivityKey,
            value
        );

        SaveSettings();
    }

    // =========================================================
    // VERTICAL SENSITIVITY
    // =========================================================

    public void SetVerticalSensitivity(float value)
    {
        VerticalSensitivity = value;

        PlayerPrefs.SetFloat(
            VerticalSensitivityKey,
            value
        );

        SaveSettings();
    }

    // =========================================================
    // CROSSHAIR
    // =========================================================

    public void SetCrosshairEnabled(bool enabled)
    {
        CrosshairEnabled = enabled;

        PlayerPrefs.SetInt(
            CrosshairKey,
            enabled ? 1 : 0
        );

        SaveSettings();
    }

    // =========================================================
    // STARTING CAMERA
    // =========================================================

    public void SetStartingCamera(int cameraIndex)
    {
        StartingCamera = cameraIndex;

        PlayerPrefs.SetInt(
            StartingCameraKey,
            cameraIndex
        );

        SaveSettings();
    }

    // =========================================================
    // MASTER VOLUME
    // =========================================================

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            MasterVolumeKey,
            MasterVolume
        );

        SaveSettings();
    }

    // =========================================================
    // MUSIC VOLUME
    // =========================================================

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            MusicVolumeKey,
            MusicVolume
        );

        SaveSettings();
    }

    // =========================================================
    // SFX VOLUME
    // =========================================================

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            SFXVolumeKey,
            SFXVolume
        );

        SaveSettings();
    }

    // =========================================================
    // SAVE
    // =========================================================

    private void SaveSettings()
    {
        PlayerPrefs.Save();

        OnSettingsChanged?.Invoke();
    }
}