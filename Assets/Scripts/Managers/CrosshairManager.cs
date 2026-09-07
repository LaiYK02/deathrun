using UnityEngine;

public class CrosshairManager : MonoBehaviour
{
    [SerializeField] private GameObject crosshair;

    private void Start()
    {
        ApplyCrosshairSetting();
    }

    private void OnEnable()
    {
        if (GameSettingsManager.Instance != null)
        {
            GameSettingsManager.Instance.OnSettingsChanged +=
                ApplyCrosshairSetting;
        }
    }

    private void OnDisable()
    {
        if (GameSettingsManager.Instance != null)
        {
            GameSettingsManager.Instance.OnSettingsChanged -=
                ApplyCrosshairSetting;
        }
    }

    private void ApplyCrosshairSetting()
    {
        if (crosshair == null)
            return;

        bool enabled =
            GameSettingsManager.Instance == null ||
            GameSettingsManager.Instance.CrosshairEnabled;

        crosshair.SetActive(enabled);
    }
}