using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera thirdPersonCamera;
    [SerializeField] private CinemachineCamera firstPersonCamera;

    [Header("Main Camera")]
    [SerializeField] private CinemachineBrain cinemachineBrain;

    [Header("Camera Priority")]
    [SerializeField] private int activePriority = 10;
    [SerializeField] private int inactivePriority = 0;

    [Header("Camera Target")]
    [SerializeField] private float targetSearchDelay = 0.2f;

    [Header("Third Person Camera Reset")]
    [SerializeField] private float thirdPersonVerticalAngle = 0f;

    // Camera should be behind the player after respawn.
    [SerializeField] private float thirdPersonCameraAngleOffset = 0f;

    public bool IsFirstPerson { get; private set; }

    private bool camerasBound = false;

    private Transform localPlayer;
    private Transform firstPersonTarget;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SetupInstantCameraSwitch();
    }

    private void Start()
    {
        SetThirdPersonView();

        // Network player may not exist yet.
        InvokeRepeating(
            nameof(TryBindToLocalPlayer),
            0.1f,
            targetSearchDelay
        );
    }

    private void Update()
    {
        if (InputManager.Instance == null)
            return;

        if (InputManager.Instance.CameraTogglePressed)
        {
            ToggleCameraView();
        }
    }

    // =========================================================
    // FIND LOCAL PLAYER
    // =========================================================

    private void TryBindToLocalPlayer()
    {
        if (camerasBound)
            return;

        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsClient)
            return;

        NetworkObject localPlayerObject =
            NetworkManager.Singleton.LocalClient?.PlayerObject;

        if (localPlayerObject == null)
            return;

        BindCamerasToPlayer(localPlayerObject.transform);

        camerasBound = true;

        CancelInvoke(nameof(TryBindToLocalPlayer));
    }

    // =========================================================
    // BIND CAMERAS
    // =========================================================

    private void BindCamerasToPlayer(Transform player)
    {
        if (player == null)
            return;

        localPlayer = player;

        // Find the first-person target inside the local player.
        firstPersonTarget =
            player.Find("FirstPerson Target");

        if (firstPersonTarget == null)
        {
            Debug.LogWarning(
                "CameraManager: FirstPerson Target was not found."
            );
        }

        // -----------------------------------------------------
        // THIRD PERSON CAMERA
        // -----------------------------------------------------

        if (thirdPersonCamera != null)
        {
            Transform target =
                firstPersonTarget != null
                    ? firstPersonTarget
                    : player;

            thirdPersonCamera.Follow = target;
            thirdPersonCamera.LookAt = target;
        }

        // -----------------------------------------------------
        // FIRST PERSON CAMERA
        // -----------------------------------------------------

        if (firstPersonCamera != null)
        {
            Transform target =
                firstPersonTarget != null
                    ? firstPersonTarget
                    : player;

            firstPersonCamera.Follow = target;
            firstPersonCamera.LookAt = target;
        }

        Debug.Log(
            $"CameraManager: Cameras bound to {player.name}."
        );
    }

    // =========================================================
    // CAMERA SWITCHING
    // =========================================================

    public void ToggleCameraView()
    {
        if (IsFirstPerson)
        {
            SetThirdPersonView();
        }
        else
        {
            SetFirstPersonView();
        }
    }

    private void SetThirdPersonView()
    {
        IsFirstPerson = false;

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.Priority = activePriority;
        }

        if (firstPersonCamera != null)
        {
            firstPersonCamera.Priority = inactivePriority;
        }
    }

    private void SetFirstPersonView()
    {
        IsFirstPerson = true;

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.Priority = inactivePriority;
        }

        if (firstPersonCamera != null)
        {
            firstPersonCamera.Priority = activePriority;
        }
    }

    // =========================================================
    // RESET CAMERA AFTER RESPAWN
    // =========================================================

    public void ResetThirdPersonCamera(
        Transform player,
        Quaternion respawnRotation)
    {
        if (player == null)
            return;

        if (thirdPersonCamera == null)
            return;

        // -----------------------------------------------------
        // Make sure the camera is following THIS local player.
        // -----------------------------------------------------

        Transform target =
            player.Find("FirstPerson Target");

        if (target == null)
        {
            target = player;
        }

        thirdPersonCamera.Follow = target;
        thirdPersonCamera.LookAt = target;

        // -----------------------------------------------------
        // Reset Cinemachine Orbital Follow.
        // -----------------------------------------------------

        CinemachineOrbitalFollow orbitalFollow =
            thirdPersonCamera.GetComponent<CinemachineOrbitalFollow>();

        if (orbitalFollow != null)
        {
            // Player's respawn direction.
            float playerYaw =
                respawnRotation.eulerAngles.y;

            // Put the camera behind the player.
            float cameraYaw =
                playerYaw + thirdPersonCameraAngleOffset;

            // Normalize angle to 0-360.
            cameraYaw =
                Mathf.Repeat(cameraYaw, 360f);

            orbitalFollow.HorizontalAxis.Value =
                cameraYaw;

            orbitalFollow.VerticalAxis.Value =
                thirdPersonVerticalAngle;
        }

        // -----------------------------------------------------
        // Tell Cinemachine not to use the previous camera state.
        // This makes the reset happen immediately.
        // -----------------------------------------------------

        thirdPersonCamera.PreviousStateIsValid = false;

        Debug.Log(
            $"CameraManager: Third-person camera reset. " +
            $"Player yaw = {respawnRotation.eulerAngles.y}"
        );
    }

    // =========================================================
    // CINEMACHINE
    // =========================================================

    private void SetupInstantCameraSwitch()
    {
        if (cinemachineBrain == null)
        {
            Debug.LogError(
                "CameraManager: Cinemachine Brain is not assigned."
            );

            return;
        }

        cinemachineBrain.DefaultBlend =
            new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.Cut,
                0f
            );
    }
}