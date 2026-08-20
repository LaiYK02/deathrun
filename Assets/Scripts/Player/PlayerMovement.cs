using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -20f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;

    private Vector3 velocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleGravity();
        HandleJump();
    }

    private void HandleMovement()
    {
        if (InputManager.Instance == null)
            return;

        Vector2 input = InputManager.Instance.MoveInput;

        if (cameraTransform == null)
            return;

        // Get camera directions.
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // Remove vertical camera rotation.
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        // Convert WASD input into camera-relative movement.
        Vector3 moveDirection =
            cameraForward * input.y +
            cameraRight * input.x;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.Normalize();

            // Move the character.
            characterController.Move(
                moveDirection * moveSpeed * Time.deltaTime
            );

            // Rotate character toward movement direction.
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void HandleJump()
    {
        if (InputManager.Instance == null)
            return;

        if (!characterController.isGrounded)
            return;

        if (InputManager.Instance.JumpPressed)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void HandleGravity()
    {
        if (characterController.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        characterController.Move(
            velocity * Time.deltaTime
        );
    }
}