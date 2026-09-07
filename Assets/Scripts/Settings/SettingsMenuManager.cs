using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenuManager : MonoBehaviour
{
    // =========================================================
    // MAIN SETTINGS PANELS
    // =========================================================

    [Header("Settings Panels")]
    [SerializeField] private GameObject gameSettingsPanel;
    [SerializeField] private GameObject soundSettingsPanel;

    // =========================================================
    // TAB BUTTONS
    // =========================================================

    [Header("Tab Buttons")]
    [SerializeField] private Button gameSettingsButton;
    [SerializeField] private Button soundSettingsButton;

    // =========================================================
    // GAME SETTINGS
    // =========================================================

    [Header("Game Settings")]
    [SerializeField] private Slider horizontalSensitivitySlider;
    [SerializeField] private Slider verticalSensitivitySlider;

    [SerializeField] private Toggle crosshairToggle;

    [SerializeField] private TMP_Dropdown startingCameraDropdown;

    // =========================================================
    // SOUND SETTINGS
    // =========================================================

    [Header("Sound Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    // =========================================================
    // BACK BUTTON
    // =========================================================

    [Header("Back")]
    [SerializeField] private Button backButton;

    // =========================================================
    // SETTINGS MANAGER
    // =========================================================

    private GameSettingsManager settingsManager;

    public event Action OnSettingsClosed;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        settingsManager = GameSettingsManager.Instance;

        if (settingsManager == null)
        {
            Debug.LogError(
                "SettingsMenuManager: " +
                "GameSettingsManager was not found."
            );

            return;
        }

        // -----------------------------------------------------
        // BUTTONS
        // -----------------------------------------------------

        if (gameSettingsButton != null)
        {
            gameSettingsButton.onClick.AddListener(
                ShowGameSettings
            );
        }

        if (soundSettingsButton != null)
        {
            soundSettingsButton.onClick.AddListener(
                ShowSoundSettings
            );
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(
                CloseSettings
            );
        }

        // -----------------------------------------------------
        // SLIDERS
        // -----------------------------------------------------

        if (horizontalSensitivitySlider != null)
        {
            horizontalSensitivitySlider.onValueChanged.AddListener(
                OnHorizontalSensitivityChanged
            );
        }

        if (verticalSensitivitySlider != null)
        {
            verticalSensitivitySlider.onValueChanged.AddListener(
                OnVerticalSensitivityChanged
            );
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(
                OnMasterVolumeChanged
            );
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(
                OnMusicVolumeChanged
            );
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(
                OnSFXVolumeChanged
            );
        }

        // -----------------------------------------------------
        // TOGGLE
        // -----------------------------------------------------

        if (crosshairToggle != null)
        {
            crosshairToggle.onValueChanged.AddListener(
                OnCrosshairChanged
            );
        }

        // -----------------------------------------------------
        // DROPDOWN
        // -----------------------------------------------------

        if (startingCameraDropdown != null)
        {
            startingCameraDropdown.onValueChanged.AddListener(
                OnStartingCameraChanged
            );
        }

        // -----------------------------------------------------
        // LOAD CURRENT VALUES
        // -----------------------------------------------------

        RefreshUI();

        ShowGameSettings();
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (gameSettingsButton != null)
        {
            gameSettingsButton.onClick.RemoveListener(
                ShowGameSettings
            );
        }

        if (soundSettingsButton != null)
        {
            soundSettingsButton.onClick.RemoveListener(
                ShowSoundSettings
            );
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(
                CloseSettings
            );
        }

        if (horizontalSensitivitySlider != null)
        {
            horizontalSensitivitySlider.onValueChanged.RemoveListener(
                OnHorizontalSensitivityChanged
            );
        }

        if (verticalSensitivitySlider != null)
        {
            verticalSensitivitySlider.onValueChanged.RemoveListener(
                OnVerticalSensitivityChanged
            );
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(
                OnMasterVolumeChanged
            );
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(
                OnMusicVolumeChanged
            );
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(
                OnSFXVolumeChanged
            );
        }

        if (crosshairToggle != null)
        {
            crosshairToggle.onValueChanged.RemoveListener(
                OnCrosshairChanged
            );
        }

        if (startingCameraDropdown != null)
        {
            startingCameraDropdown.onValueChanged.RemoveListener(
                OnStartingCameraChanged
            );
        }
    }

    // =========================================================
    // OPEN SETTINGS
    // =========================================================

    public void OpenSettings()
    {
        gameObject.SetActive(true);

        RefreshUI();

        ShowGameSettings();
    }

    // =========================================================
    // CLOSE SETTINGS
    // =========================================================

    public void CloseSettings()
    {
        gameObject.SetActive(false);

        OnSettingsClosed?.Invoke();
    }

    // =========================================================
    // GAME SETTINGS PANEL
    // =========================================================

    public void ShowGameSettings()
    {
        if (gameSettingsPanel != null)
        {
            gameSettingsPanel.SetActive(true);
        }

        if (soundSettingsPanel != null)
        {
            soundSettingsPanel.SetActive(false);
        }
    }

    // =========================================================
    // SOUND SETTINGS PANEL
    // =========================================================

    public void ShowSoundSettings()
    {
        if (gameSettingsPanel != null)
        {
            gameSettingsPanel.SetActive(false);
        }

        if (soundSettingsPanel != null)
        {
            soundSettingsPanel.SetActive(true);
        }
    }

    // =========================================================
    // REFRESH UI
    // =========================================================

    private void RefreshUI()
    {
        if (settingsManager == null)
            return;

        // -----------------------------------------------------
        // GAME SETTINGS
        // -----------------------------------------------------

        if (horizontalSensitivitySlider != null)
        {
            horizontalSensitivitySlider.SetValueWithoutNotify(
                settingsManager.HorizontalSensitivity
            );
        }

        if (verticalSensitivitySlider != null)
        {
            verticalSensitivitySlider.SetValueWithoutNotify(
                settingsManager.VerticalSensitivity
            );
        }

        if (crosshairToggle != null)
        {
            crosshairToggle.SetIsOnWithoutNotify(
                settingsManager.CrosshairEnabled
            );
        }

        if (startingCameraDropdown != null)
        {
            startingCameraDropdown.SetValueWithoutNotify(
                settingsManager.StartingCamera
            );
        }

        // -----------------------------------------------------
        // SOUND SETTINGS
        // -----------------------------------------------------

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(
                settingsManager.MasterVolume
            );
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(
                settingsManager.MusicVolume
            );
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(
                settingsManager.SFXVolume
            );
        }
    }

    // =========================================================
    // GAME SETTINGS CALLBACKS
    // =========================================================

    private void OnHorizontalSensitivityChanged(float value)
    {
        settingsManager.SetHorizontalSensitivity(value);
    }

    private void OnVerticalSensitivityChanged(float value)
    {
        settingsManager.SetVerticalSensitivity(value);
    }

    private void OnCrosshairChanged(bool enabled)
    {
        settingsManager.SetCrosshairEnabled(enabled);
    }

    private void OnStartingCameraChanged(int value)
    {
        settingsManager.SetStartingCamera(value);
    }

    // =========================================================
    // SOUND SETTINGS CALLBACKS
    // =========================================================

    private void OnMasterVolumeChanged(float value)
    {
        settingsManager.SetMasterVolume(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        settingsManager.SetMusicVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        settingsManager.SetSFXVolume(value);
    }
}