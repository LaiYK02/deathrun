using UnityEngine;

public class PlayerLookManager : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float horizontalSensitivity = 0.2f;
    [SerializeField] private float verticalSensitivity = 0.08f;

    [Header("Vertical Look")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("First Person Camera")]
    [SerializeField] private Transform firstPersonTarget;

    private float pitch;

    private void Start()
    {
        if (firstPersonTarget != null)
        {
            pitch = firstPersonTarget.localEulerAngles.x;

            if (pitch > 180f)
                pitch -= 360f;
        }
    }

    private void Update()
    {
        if (InputManager.Instance == null)
            return;

        HandleLook();
    }

    private void HandleLook()
    {
        Vector2 lookInput = InputManager.Instance.LookInput;

        if (lookInput.sqrMagnitude <= 0.001f)
            return;

        float mouseX = lookInput.x * horizontalSensitivity;
        float mouseY = lookInput.y * verticalSensitivity;

        // Horizontal rotation rotates the player.
        transform.Rotate(Vector3.up * mouseX);

        // Vertical rotation controls the first-person camera target.
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (firstPersonTarget != null)
        {
            firstPersonTarget.localRotation = Quaternion.Euler(
                pitch,
                0f,
                0f
            );
        }
    }
}