using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
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
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    [Header("Jump Buffer")]
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Player Rotation")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;

    private Vector3 velocity;

    private float jumpBufferTimer;

    public MovementState CurrentState => currentState;

    public Vector3 Velocity => velocity;

    public float CurrentSpeed => new Vector3(
        velocity.x,
        0f,
        velocity.z
    ).magnitude;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();
    }

    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only the local player needs a camera reference.
        if (IsOwner)
        {
            RefreshCameraReference();
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // =========================================================
    // NETWORK DESPAWN
    // =========================================================

    public override void OnNetworkDespawn()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        base.OnNetworkDespawn();
    }

    // =========================================================
    // SCENE LOADED
    // =========================================================

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if (!IsOwner)
            return;

        // The old Lobby camera was destroyed.
        // Get the new GameScene camera.
        RefreshCameraReference();
    }

    // =========================================================
    // CAMERA
    // =========================================================

    private void RefreshCameraReference()
    {
        if (!IsOwner)
            return;

        if (Camera.main != null)
        {
            cameraTransform =
                Camera.main.transform;

            Debug.Log(
                $"PlayerMovement: Camera reference updated to " +
                $"{Camera.main.name}."
            );
        }
        else
        {
            cameraTransform = null;

            Debug.LogWarning(
                "PlayerMovement: Main Camera not found."
            );
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!IsOwner)
            return;

        // Make sure we always have a valid camera.
        if (cameraTransform == null)
        {
            RefreshCameraReference();

            if (cameraTransform == null)
                return;
        }

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

    // =========================================================
    // JUMP BUFFER
    // =========================================================

    private void UpdateJumpBuffer()
    {
        if (InputManager.Instance != null &&
            InputManager.Instance.JumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    // =========================================================
    // MOVEMENT STATE
    // =========================================================

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

    // =========================================================
    // GROUND MOVEMENT
    // =========================================================

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
    }

    // =========================================================
    // AIR MOVEMENT
    // =========================================================

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

    // =========================================================
    // INPUT
    // =========================================================

    private Vector2 GetMoveInput()
    {
        if (InputManager.Instance == null)
            return Vector2.zero;

        return InputManager.Instance.MoveInput;
    }

    // =========================================================
    // WISH DIRECTION
    // =========================================================

    private void GetWishDirectionAndSpeed(
        Vector2 input,
        out Vector3 wishDirection,
        out float wishSpeed)
    {
        wishDirection = Vector3.zero;
        wishSpeed = 0f;

        if (cameraTransform == null)
            return;

        Vector3 cameraForward =
            cameraTransform.forward;

        Vector3 cameraRight =
            cameraTransform.right;

        // Keep movement horizontal.
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        if (cameraForward.sqrMagnitude <= 0.001f)
            return;

        if (cameraRight.sqrMagnitude <= 0.001f)
            return;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 wishVelocity =
            cameraForward * input.y +
            cameraRight * input.x;

        float inputMagnitude =
            wishVelocity.magnitude;

        if (inputMagnitude <= 0.001f)
            return;

        wishDirection =
            wishVelocity / inputMagnitude;

        wishSpeed =
            maxSpeed *
            Mathf.Clamp01(inputMagnitude);
    }

    // =========================================================
    // FRICTION
    // =========================================================

    private void ApplyGroundFriction()
    {
        Vector3 horizontalVelocity =
            new Vector3(
                velocity.x,
                0f,
                velocity.z
            );

        float speed =
            horizontalVelocity.magnitude;

        if (speed <= 0.001f)
            return;

        float control =
            Mathf.Max(
                speed,
                stopSpeed
            );

        float drop =
            control *
            groundFriction *
            Time.deltaTime;

        float newSpeed =
            Mathf.Max(
                speed - drop,
                0f
            );

        if (newSpeed != speed)
        {
            horizontalVelocity *=
                newSpeed / speed;

            velocity.x =
                horizontalVelocity.x;

            velocity.z =
                horizontalVelocity.z;
        }
    }

    // =========================================================
    // GROUND ACCELERATION
    // =========================================================

    private void Accelerate(
        Vector3 wishDirection,
        float wishSpeed,
        float acceleration)
    {
        float currentSpeed =
            Vector3.Dot(
                GetHorizontalVelocity(),
                wishDirection
            );

        float addSpeed =
            wishSpeed - currentSpeed;

        if (addSpeed <= 0f)
            return;

        float accelerationSpeed =
            acceleration *
            wishSpeed *
            Time.deltaTime;

        accelerationSpeed =
            Mathf.Min(
                accelerationSpeed,
                addSpeed
            );

        velocity +=
            wishDirection *
            accelerationSpeed;
    }

    // =========================================================
    // AIR ACCELERATION
    // =========================================================

    private void AirAccelerate(
        Vector3 wishDirection,
        float wishSpeed)
    {
        float currentSpeed =
            Vector3.Dot(
                GetHorizontalVelocity(),
                wishDirection
            );

        float cappedWishSpeed =
            Mathf.Min(
                wishSpeed,
                airWishSpeed
            );

        float addSpeed =
            cappedWishSpeed -
            currentSpeed;

        if (addSpeed <= 0f)
            return;

        float accelerationSpeed =
            airAcceleration *
            wishSpeed *
            Time.deltaTime;

        accelerationSpeed =
            Mathf.Min(
                accelerationSpeed,
                addSpeed
            );

        velocity +=
            wishDirection *
            accelerationSpeed;
    }

    // =========================================================
    // JUMP
    // =========================================================

    private void HandleJump()
    {
        if (jumpBufferTimer <= 0f)
            return;

        if (currentState != MovementState.Ground)
            return;

        velocity.y =
            Mathf.Sqrt(
                jumpHeight *
                -2f *
                gravity
            );

        currentState =
            MovementState.Air;

        jumpBufferTimer = 0f;
    }

    // =========================================================
    // GRAVITY
    // =========================================================

    private void HandleGravity()
    {
        if (currentState ==
            MovementState.Ground)
        {
            if (velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            return;
        }

        velocity.y +=
            gravity *
            Time.deltaTime;
    }

    // =========================================================
    // APPLY MOVEMENT
    // =========================================================

    private void ApplyMovement()
    {
        characterController.Move(
            velocity *
            Time.deltaTime
        );
    }

    // =========================================================
    // HORIZONTAL VELOCITY
    // =========================================================

    private Vector3 GetHorizontalVelocity()
    {
        return new Vector3(
            velocity.x,
            0f,
            velocity.z
        );
    }

    // =========================================================
    // RESET VELOCITY
    // =========================================================

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
        jumpBufferTimer = 0f;
    }
}