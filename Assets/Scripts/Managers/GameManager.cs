using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;

    private void Awake()
    {
        SetupDisplay();
        SetupCursor();
    }

    private void SetupDisplay()
    {
        // Start the game in fullscreen.
        Screen.fullScreenMode = fullScreenMode;
        Screen.fullScreen = true;
    }

    private void SetupCursor()
    {
        // Hide and lock the cursor in the center of the screen.
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}