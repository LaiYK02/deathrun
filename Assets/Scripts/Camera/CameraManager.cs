using UnityEngine;
using Unity.Cinemachine;

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

    public bool IsFirstPerson { get; private set; }

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

    private void SetupInstantCameraSwitch()
    {
        if (cinemachineBrain == null)
        {
            Debug.LogError("CameraManager: Cinemachine Brain is not assigned.");
            return;
        }

        // Disable blending so camera switches happen instantly.
        cinemachineBrain.DefaultBlend =
            new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.Cut,
                0f
            );
    }

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

        thirdPersonCamera.Priority = activePriority;
        firstPersonCamera.Priority = inactivePriority;
    }

    private void SetFirstPersonView()
    {
        IsFirstPerson = true;

        thirdPersonCamera.Priority = inactivePriority;
        firstPersonCamera.Priority = activePriority;
    }
}