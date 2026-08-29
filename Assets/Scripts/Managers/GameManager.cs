using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField]
    private FullScreenMode fullScreenMode =
        FullScreenMode.FullScreenWindow;

    private void Awake()
    {
        SetupDisplay();
    }

    private void SetupDisplay()
    {
        Screen.fullScreenMode = fullScreenMode;
        Screen.fullScreen = true;
    }
}