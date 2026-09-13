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

    [SerializeField] private float thirdPersonCameraAngleOffset = 0f;

    public bool IsFirstPerson { get; private set; }

    public bool IsSpectatorMode { get; private set; }

    private bool camerasBound;
    private bool cameraControlEnabled = true;

    private Transform localPlayer;
    private Transform firstPersonTarget;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SetupInstantCameraSwitch();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        ApplyStartingCamera();

        InvokeRepeating(
            nameof(TryBindToLocalPlayer),
            0.1f,
            targetSearchDelay
        );
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = true;
        }

        SetThirdPersonInputEnabled(true);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!cameraControlEnabled)
            return;

        if (IsSpectatorMode)
        {
            // V is disabled while spectating.
            return;
        }

        if (InputManager.Instance == null)
            return;

        if (InputManager.Instance.CameraTogglePressed)
        {
            ToggleCameraView();
        }
    }

    // =========================================================
    // CAMERA CONTROL
    // =========================================================

    public void SetCameraControlEnabled(
        bool enabled)
    {
        cameraControlEnabled = enabled;

        SetThirdPersonInputEnabled(
            enabled
        );

        Debug.Log(
            $"CameraManager: Camera input " +
            $"{(enabled ? "enabled" : "disabled")}."
        );
    }

    // =========================================================
    // THIRD PERSON INPUT
    // =========================================================

    private void SetThirdPersonInputEnabled(
        bool enabled)
    {
        if (thirdPersonCamera == null)
            return;

        CinemachineInputAxisController inputController =
            thirdPersonCamera.GetComponent<
                CinemachineInputAxisController
            >();

        if (inputController != null)
        {
            inputController.enabled =
                enabled;
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
            NetworkManager.Singleton
                .LocalClient?
                .PlayerObject;

        if (localPlayerObject == null)
            return;

        BindCamerasToPlayer(
            localPlayerObject.transform
        );

        camerasBound = true;

        CancelInvoke(
            nameof(TryBindToLocalPlayer)
        );
    }

    // =========================================================
    // BIND CAMERAS
    // =========================================================

    private void BindCamerasToPlayer(
        Transform player)
    {
        if (player == null)
            return;

        localPlayer = player;

        firstPersonTarget =
            player.Find("FirstPerson Target");

        Transform target =
            firstPersonTarget != null
                ? firstPersonTarget
                : player;

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.Follow =
                target;

            thirdPersonCamera.LookAt =
                target;
        }

        if (firstPersonCamera != null)
        {
            firstPersonCamera.Follow =
                target;

            firstPersonCamera.LookAt =
                target;
        }

        Debug.Log(
            $"CameraManager: Cameras bound to " +
            $"{player.name}."
        );
    }

    // =========================================================
    // CAMERA SWITCHING
    // =========================================================

    public void ToggleCameraView()
    {
        if (!cameraControlEnabled)
            return;

        if (IsSpectatorMode)
            return;

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
            thirdPersonCamera.Priority =
                activePriority;
        }

        if (firstPersonCamera != null)
        {
            firstPersonCamera.Priority =
                inactivePriority;
        }

        SetThirdPersonInputEnabled(
            cameraControlEnabled
        );
    }

    private void SetFirstPersonView()
    {
        IsFirstPerson = true;

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.Priority =
                inactivePriority;
        }

        if (firstPersonCamera != null)
        {
            firstPersonCamera.Priority =
                activePriority;
        }

        SetThirdPersonInputEnabled(false);
    }

    // =========================================================
    // ENTER SPECTATOR MODE
    // =========================================================

    public void EnterSpectatorMode(
        Transform target)
    {
        if (target == null)
            return;

        IsSpectatorMode = true;

        cameraControlEnabled = true;

        // Force third person.
        SetThirdPersonView();

        // Follow the dead player's body first.
        SetSpectatorTarget(target);

        // Allow orbital camera movement.
        SetThirdPersonInputEnabled(true);

        Debug.Log(
            $"CameraManager: Spectator mode entered. " +
            $"Target = {target.name}"
        );
    }

    // =========================================================
    // SET SPECTATOR TARGET
    // =========================================================

    public void SetSpectatorTarget(
        Transform target)
    {
        if (target == null)
            return;

        if (thirdPersonCamera == null)
            return;

        Transform targetPoint =
            target.Find("FirstPerson Target");

        if (targetPoint == null)
        {
            targetPoint = target;
        }

        thirdPersonCamera.Follow =
            targetPoint;

        thirdPersonCamera.LookAt =
            targetPoint;

        // Make sure third person is active.
        IsFirstPerson = false;

        thirdPersonCamera.Priority =
            activePriority;

        if (firstPersonCamera != null)
        {
            firstPersonCamera.Priority =
                inactivePriority;
        }

        Debug.Log(
            $"CameraManager: Spectator target changed to " +
            $"{target.name}."
        );
    }

    // =========================================================
    // RESET FOR NEW ROUND
    // =========================================================

    public void ResetForNewRound(
        Transform player,
        Quaternion playerRotation)
    {
        if (player == null)
            return;

        IsSpectatorMode = false;

        cameraControlEnabled = true;

        camerasBound = true;

        BindCamerasToPlayer(player);

        // Reset player camera based on the user's
        // Starting Camera setting.
        ApplyStartingCamera();

        if (!IsFirstPerson)
        {
            ResetThirdPersonCamera(
                player,
                playerRotation
            );
        }

        SetThirdPersonInputEnabled(
            !IsFirstPerson
        );

        Debug.Log(
            "CameraManager: Camera reset for new round."
        );
    }

    // =========================================================
    // RESET THIRD PERSON CAMERA
    // =========================================================

    public void ResetThirdPersonCamera(
        Transform player,
        Quaternion respawnRotation)
    {
        if (player == null)
            return;

        if (thirdPersonCamera == null)
            return;

        Transform target =
            player.Find("FirstPerson Target");

        if (target == null)
        {
            target = player;
        }

        thirdPersonCamera.Follow =
            target;

        thirdPersonCamera.LookAt =
            target;

        CinemachineOrbitalFollow orbitalFollow =
            thirdPersonCamera.GetComponent<
                CinemachineOrbitalFollow
            >();

        if (orbitalFollow != null)
        {
            float playerYaw =
                respawnRotation.eulerAngles.y;

            float cameraYaw =
                playerYaw +
                thirdPersonCameraAngleOffset;

            cameraYaw =
                Mathf.Repeat(
                    cameraYaw,
                    360f
                );

            orbitalFollow.HorizontalAxis.Value =
                cameraYaw;

            orbitalFollow.VerticalAxis.Value =
                thirdPersonVerticalAngle;
        }

        if (cinemachineBrain != null &&
            cinemachineBrain.enabled)
        {
            thirdPersonCamera.PreviousStateIsValid =
                false;
        }
    }

    // =========================================================
    // CINEMACHINE
    // =========================================================

    private void SetupInstantCameraSwitch()
    {
        if (cinemachineBrain == null)
        {
            Debug.LogError(
                "CameraManager: " +
                "Cinemachine Brain is not assigned."
            );

            return;
        }

        cinemachineBrain.DefaultBlend =
            new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.Cut,
                0f
            );
    }

    // =========================================================
    // STARTING CAMERA
    // =========================================================

    private void ApplyStartingCamera()
    {
        int startingCamera = 0;

        if (GameSettingsManager.Instance != null)
        {
            startingCamera =
                GameSettingsManager.Instance.StartingCamera;
        }

        if (startingCamera == 0)
        {
            SetThirdPersonView();
        }
        else
        {
            SetFirstPersonView();
        }
    }
}