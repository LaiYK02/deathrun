using UnityEngine;
using UnityEngine.UI;

public class CrosshairManager : MonoBehaviour
{
    [Header("Crosshair")]
    [SerializeField] private GameObject crosshair;
    [SerializeField] private Image crosshairImage;

    [Header("Crosshair Sprites")]
    [SerializeField] private Sprite normalCrosshair;
    [SerializeField] private Sprite funnyCrosshair;

    private GameSettingsManager settingsManager;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        settingsManager =
            GameSettingsManager.Instance;

        ApplyCrosshairSetting();
    }

    // =========================================================
    // ENABLE
    // =========================================================

    private void OnEnable()
    {
        if (GameSettingsManager.Instance != null)
        {
            GameSettingsManager.Instance.OnSettingsChanged +=
                ApplyCrosshairSetting;
        }
    }

    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        if (GameSettingsManager.Instance != null)
        {
            GameSettingsManager.Instance.OnSettingsChanged -=
                ApplyCrosshairSetting;
        }
    }

    // =========================================================
    // APPLY CROSSHAIR
    // =========================================================

    private void ApplyCrosshairSetting()
    {
        if (crosshair == null)
            return;

        settingsManager =
            GameSettingsManager.Instance;

        if (settingsManager == null)
        {
            crosshair.SetActive(true);
            return;
        }

        // -----------------------------------------------------
        // CROSSHAIR ENABLE / DISABLE
        // -----------------------------------------------------

        bool crosshairEnabled =
            settingsManager.CrosshairEnabled;

        crosshair.SetActive(crosshairEnabled);

        if (!crosshairEnabled)
            return;

        // -----------------------------------------------------
        // CHOOSE CROSSHAIR
        // -----------------------------------------------------

        if (crosshairImage == null)
            return;

        Sprite targetSprite;

        if (settingsManager.VeryFunnyMode)
        {
            targetSprite = funnyCrosshair;
        }
        else
        {
            targetSprite = normalCrosshair;
        }

        // -----------------------------------------------------
        // APPLY SPRITE
        // -----------------------------------------------------

        if (targetSprite != null)
        {
            crosshairImage.sprite = targetSprite;
        }
    }
}