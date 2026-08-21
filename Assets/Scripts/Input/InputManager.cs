using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    public bool JumpPressed { get; private set; }
    public bool CameraTogglePressed { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        ReadMovementInput();
        ReadLookInput();
        ReadJumpInput();
        ReadCameraInput();
    }

    private void ReadMovementInput()
    {
        if (Keyboard.current == null)
        {
            MoveInput = Vector2.zero;
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.aKey.isPressed)
            horizontal -= 1f;

        if (Keyboard.current.dKey.isPressed)
            horizontal += 1f;

        if (Keyboard.current.sKey.isPressed)
            vertical -= 1f;

        if (Keyboard.current.wKey.isPressed)
            vertical += 1f;

        MoveInput = new Vector2(horizontal, vertical);

        // Prevent diagonal movement from being faster.
        MoveInput = Vector2.ClampMagnitude(MoveInput, 1f);
    }

    private void ReadLookInput()
    {
        if (Mouse.current == null)
        {
            LookInput = Vector2.zero;
            return;
        }

        LookInput = Mouse.current.delta.ReadValue();
    }

    private void ReadJumpInput()
    {
        JumpPressed = Keyboard.current != null &&
                      Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    private void ReadCameraInput()
    {
        CameraTogglePressed = Keyboard.current != null &&
                              Keyboard.current.vKey.wasPressedThisFrame;
    }
}