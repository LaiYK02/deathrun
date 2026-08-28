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

    public bool IsFirstPerson { get; private set; }

    private bool camerasBound = false;

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

        NetworkObject localPlayer =
            NetworkManager.Singleton.LocalClient?.PlayerObject;

        if (localPlayer == null)
            return;

        BindCamerasToPlayer(localPlayer.transform);

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

        // Find the first-person target inside the local player.
        Transform firstPersonTarget =
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