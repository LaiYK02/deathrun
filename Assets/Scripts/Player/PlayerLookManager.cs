using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerLookManager : NetworkBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float horizontalSensitivity = 0.2f;
    [SerializeField] private float verticalSensitivity = 0.08f;

    [Header("Vertical Look")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("First Person Camera")]
    [SerializeField] private Transform firstPersonTarget;

    [Header("First Person Model")]
    [SerializeField] private GameObject playerModel;

    [Header("Third Person Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float playerRotationSpeed = 10f;

    [Header("Gameplay Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    private bool lastFirstPersonState;

    private float pitch;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (firstPersonTarget != null)
        {
            pitch =
                firstPersonTarget.localEulerAngles.x;

            if (pitch > 180f)
            {
                pitch -= 360f;
            }
        }

        // Only the local player controls
        // their own camera/model.
        if (!IsOwner)
        {
            SetPlayerModelVisible(true);
            return;
        }

        if (cameraTransform == null &&
            Camera.main != null)
        {
            cameraTransform =
                Camera.main.transform;
        }

        if (CameraManager.Instance != null)
        {
            lastFirstPersonState =
                CameraManager.Instance.IsFirstPerson;

            SetPlayerModelVisible(
                !lastFirstPersonState
            );
        }

        // Set correct cursor state.
        UpdateCursor();
    }

    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SceneManager.sceneLoaded +=
            OnSceneLoaded;

        // Set cursor immediately if this is
        // the local player.
        if (IsOwner)
        {
            UpdateCursor();
        }
    }

    // =========================================================
    // NETWORK DESPAWN
    // =========================================================

    public override void OnNetworkDespawn()
    {
        SceneManager.sceneLoaded -=
            OnSceneLoaded;

        // Make sure cursor is restored
        // when the player disappears.
        if (IsOwner)
        {
            ShowCursor();
        }

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

        // Refresh camera reference because
        // the old Lobby camera was destroyed.
        if (Camera.main != null)
        {
            cameraTransform =
                Camera.main.transform;

            Debug.Log(
                $"PlayerLookManager: Camera reference updated to " +
                $"{Camera.main.name}."
            );
        }

        // Update cursor for the new scene.
        UpdateCursor();
    }

    // =========================================================
    // CURSOR
    // =========================================================

    private void UpdateCursor()
    {
        if (!IsOwner)
            return;

        if (SceneManager.GetActiveScene().name ==
            gameSceneName)
        {
            LockCursor();
        }
        else
        {
            ShowCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Remote players should always be visible
        // and should never control the local camera.
        if (!IsOwner)
        {
            if (playerModel != null)
            {
                SetPlayerModelVisible(true);
            }

            return;
        }

        if (InputManager.Instance == null)
            return;

        if (CameraManager.Instance == null)
            return;

        // Detect camera mode change.
        bool currentFirstPersonState =
            CameraManager.Instance.IsFirstPerson;

        if (currentFirstPersonState !=
            lastFirstPersonState)
        {
            lastFirstPersonState =
                currentFirstPersonState;

            SetPlayerModelVisible(
                !currentFirstPersonState
            );
        }

        if (CameraManager.Instance.IsFirstPerson)
        {
            HandleFirstPersonLook();
        }
        else
        {
            HandleThirdPersonLook();
        }
    }

    // =========================================================
    // FIRST PERSON LOOK
    // =========================================================

    private void HandleFirstPersonLook()
    {
        Vector2 lookInput =
            InputManager.Instance.LookInput;

        float mouseX =
            lookInput.x *
            horizontalSensitivity;

        float mouseY =
            lookInput.y *
            verticalSensitivity;

        // Horizontal player rotation.
        transform.Rotate(
            Vector3.up * mouseX
        );

        // Vertical camera rotation.
        pitch -= mouseY;

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );

        if (firstPersonTarget != null)
        {
            firstPersonTarget.localRotation =
                Quaternion.Euler(
                    pitch,
                    0f,
                    0f
                );
        }
    }

    // =========================================================
    // PLAYER MODEL VISIBILITY
    // =========================================================

    private void SetPlayerModelVisible(
        bool visible)
    {
        if (playerModel == null)
            return;

        Renderer[] renderers =
            playerModel.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
    }

    // =========================================================
    // THIRD PERSON LOOK
    // =========================================================

    private void HandleThirdPersonLook()
    {
        if (cameraTransform == null)
            return;

        Vector3 cameraForward =
            cameraTransform.forward;

        // Ignore camera's vertical pitch.
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude >
            0.001f)
        {
            cameraForward.Normalize();

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    cameraForward,
                    Vector3.up
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    playerRotationSpeed *
                    Time.deltaTime
                );
        }
    }

    // =========================================================
    // RESET LOOK AFTER RESPAWN
    // =========================================================

    public void ResetLook(
        Quaternion respawnRotation)
    {
        // Reset player horizontal rotation.
        transform.rotation =
            respawnRotation;

        // Reset vertical look.
        pitch = 0f;

        // Reset first-person target.
        if (firstPersonTarget != null)
        {
            firstPersonTarget.localRotation =
                Quaternion.identity;
        }

        // Reset camera reference.
        if (cameraTransform == null &&
            Camera.main != null)
        {
            cameraTransform =
                Camera.main.transform;
        }
    }
}