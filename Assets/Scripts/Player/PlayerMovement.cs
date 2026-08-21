using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public enum MovementState
    {
        Ground,
        Air
    }

    [Header("Movement State")]
    [SerializeField] private MovementState currentState;

    [Header("Speed")]
    [SerializeField] private float maxSpeed = 7f;

    [Header("Ground Movement")]
    [SerializeField] private float groundAcceleration = 30f;
    [SerializeField] private float groundFriction = 8f;
    [SerializeField] private float stopSpeed = 2f;

    [Header("Air Movement")]
    [SerializeField] private float airAcceleration = 12f;
    [SerializeField] private float airWishSpeed = 1.5f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -20f;

    [Header("Jump Buffer")]
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Player Rotation")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;

    private Vector3 velocity;

    // How long the player's jump input remains valid.
    private float jumpBufferTimer;

    public MovementState CurrentState => currentState;

    public Vector3 Velocity => velocity;

    public float CurrentSpeed => new Vector3(
        velocity.x,
        0f,
        velocity.z
    ).magnitude;

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
        UpdateJumpBuffer();
        UpdateMovementState();

        switch (currentState)
        {
            case MovementState.Ground:
                HandleGroundMovement();
                break;

            case MovementState.Air:
                HandleAirMovement();
                break;
        }

        HandleJump();
        HandleGravity();
        ApplyMovement();
    }

    private void UpdateJumpBuffer()
    {
        // Store the jump input.
        if (InputManager.Instance != null &&
            InputManager.Instance.JumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else if (jumpBufferTimer > 0f)
        {
            // Countdown the buffered jump request.
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void UpdateMovementState()
    {
        if (characterController.isGrounded)
        {
            currentState = MovementState.Ground;
        }
        else
        {
            currentState = MovementState.Air;
        }
    }

    private void HandleGroundMovement()
    {
        ApplyGroundFriction();

        Vector2 input = GetMoveInput();

        if (input.sqrMagnitude <= 0.001f)
            return;

        GetWishDirectionAndSpeed(
            input,
            out Vector3 wishDirection,
            out float wishSpeed
        );

        Accelerate(
            wishDirection,
            wishSpeed,
            groundAcceleration
        );

        RotatePlayer(wishDirection);
    }

    private void HandleAirMovement()
    {
        Vector2 input = GetMoveInput();

        if (input.sqrMagnitude <= 0.001f)
            return;

        GetWishDirectionAndSpeed(
            input,
            out Vector3 wishDirection,
            out float wishSpeed
        );

        AirAccelerate(
            wishDirection,
            wishSpeed
        );
    }

    private Vector2 GetMoveInput()
    {
        if (InputManager.Instance == null)
            return Vector2.zero;

        return InputManager.Instance.MoveInput;
    }

    private void GetWishDirectionAndSpeed(
        Vector2 input,
        out Vector3 wishDirection,
        out float wishSpeed)
    {
        wishDirection = Vector3.zero;
        wishSpeed = 0f;

        if (cameraTransform == null)
            return;

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // Movement should stay on the horizontal plane.
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 wishVelocity =
            cameraForward * input.y +
            cameraRight * input.x;

        float inputMagnitude = wishVelocity.magnitude;

        if (inputMagnitude <= 0.001f)
            return;

        wishDirection = wishVelocity / inputMagnitude;

        // Input magnitude determines desired speed.
        wishSpeed = maxSpeed * Mathf.Clamp01(inputMagnitude);
    }

    private void ApplyGroundFriction()
    {
        Vector3 horizontalVelocity = new Vector3(
            velocity.x,
            0f,
            velocity.z
        );

        float speed = horizontalVelocity.magnitude;

        if (speed <= 0.001f)
            return;

        // Source-style friction control.
        float control = Mathf.Max(speed, stopSpeed);

        float drop =
            control *
            groundFriction *
            Time.deltaTime;

        float newSpeed = Mathf.Max(
            speed - drop,
            0f
        );

        if (newSpeed != speed)
        {
            horizontalVelocity *= newSpeed / speed;

            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
        }
    }

    private void Accelerate(
        Vector3 wishDirection,
        float wishSpeed,
        float acceleration)
    {
        float currentSpeed = Vector3.Dot(
            GetHorizontalVelocity(),
            wishDirection
        );

        float addSpeed = wishSpeed - currentSpeed;

        if (addSpeed <= 0f)
            return;

        float accelerationSpeed =
            acceleration *
            wishSpeed *
            Time.deltaTime;

        accelerationSpeed = Mathf.Min(
            accelerationSpeed,
            addSpeed
        );

        velocity += wishDirection * accelerationSpeed;
    }

    private void AirAccelerate(
        Vector3 wishDirection,
        float wishSpeed)
    {
        float currentSpeed = Vector3.Dot(
            GetHorizontalVelocity(),
            wishDirection
        );

        float cappedWishSpeed = Mathf.Min(
            wishSpeed,
            airWishSpeed
        );

        float addSpeed = cappedWishSpeed - currentSpeed;

        if (addSpeed <= 0f)
            return;

        float accelerationSpeed =
            airAcceleration *
            wishSpeed *
            Time.deltaTime;

        accelerationSpeed = Mathf.Min(
            accelerationSpeed,
            addSpeed
        );

        velocity += wishDirection * accelerationSpeed;
    }

    private void HandleJump()
    {
        // No jump request available.
        if (jumpBufferTimer <= 0f)
            return;

        // Player isn't on the ground yet.
        if (currentState != MovementState.Ground)
            return;

        // Execute the buffered jump.
        velocity.y = Mathf.Sqrt(
            jumpHeight * -2f * gravity
        );

        currentState = MovementState.Air;

        // Consume the buffered jump.
        jumpBufferTimer = 0f;
    }

    private void HandleGravity()
    {
        if (currentState == MovementState.Ground)
        {
            // Keep the CharacterController grounded.
            if (velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            return;
        }

        velocity.y += gravity * Time.deltaTime;
    }

    private void ApplyMovement()
    {
        characterController.Move(
            velocity * Time.deltaTime
        );
    }

    private Vector3 GetHorizontalVelocity()
    {
        return new Vector3(
            velocity.x,
            0f,
            velocity.z
        );
    }

    private void RotatePlayer(Vector3 wishDirection)
    {
        // First person:
        // PlayerLookManager controls horizontal rotation.
        if (CameraManager.Instance != null &&
            CameraManager.Instance.IsFirstPerson)
        {
            return;
        }

        if (wishDirection.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(wishDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
        jumpBufferTimer = 0f;
    }
}