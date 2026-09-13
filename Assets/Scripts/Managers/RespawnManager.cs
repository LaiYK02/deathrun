using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class RespawnManager : NetworkBehaviour
{
    [Header("Player Model")]
    [SerializeField] private Transform playerModel;

    [Header("Death Audio")]
    [SerializeField] private AudioSource deathAudioSource;
    [SerializeField] private AudioClip normalDeathSound;
    [SerializeField] private AudioClip funnyDeathSound;

    [Header("Gameplay Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    // ---------------------------------------------------------
    // NETWORK DEATH STATE
    // ---------------------------------------------------------

    public NetworkVariable<bool> IsDead =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // ---------------------------------------------------------
    // LOCAL STATE
    // ---------------------------------------------------------

    public bool DeathTriggeredLocally
    {
        get;
        private set;
    }

    private CharacterController characterController;

    private Vector3 modelLocalPosition;
    private Quaternion modelLocalRotation;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private bool gameplayInitialized;
    private bool deathRequested;

    private Coroutine initializeCoroutine;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();

        if (playerModel != null)
        {
            modelLocalPosition =
                playerModel.localPosition;

            modelLocalRotation =
                playerModel.localRotation;
        }

        if (deathAudioSource != null)
        {
            deathAudioSource.playOnAwake = false;
            deathAudioSource.loop = false;
            deathAudioSource.spatialBlend = 1f;
        }
    }

    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        IsDead.OnValueChanged +=
            OnDeathStateChanged;

        SceneManager.sceneLoaded +=
            OnSceneLoaded;

        if (SceneManager.GetActiveScene().name ==
            gameSceneName)
        {
            StartGameplayInitialization();
        }
    }

    // =========================================================
    // NETWORK DESPAWN
    // =========================================================

    public override void OnNetworkDespawn()
    {
        IsDead.OnValueChanged -=
            OnDeathStateChanged;

        SceneManager.sceneLoaded -=
            OnSceneLoaded;

        if (initializeCoroutine != null)
        {
            StopCoroutine(initializeCoroutine);
            initializeCoroutine = null;
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
        if (scene.name != gameSceneName)
            return;

        StartGameplayInitialization();
    }

    // =========================================================
    // GAMEPLAY INITIALIZATION
    // =========================================================

    private void StartGameplayInitialization()
    {
        if (!IsSpawned)
            return;

        if (gameplayInitialized)
            return;

        if (initializeCoroutine != null)
            return;

        initializeCoroutine =
            StartCoroutine(
                InitializeGameplayAfterSceneLoad()
            );
    }

    private IEnumerator InitializeGameplayAfterSceneLoad()
    {
        yield return null;

        while (SpawnPointsManager.Instance == null)
        {
            yield return null;
        }

        initializeCoroutine = null;

        if (gameplayInitialized)
            yield break;

        gameplayInitialized = true;

        if (IsServer)
        {
            AssignSpawnPoint();

            SendInitialSpawnToOwner();
        }
    }

    // =========================================================
    // ASSIGN SPAWN POINT
    // =========================================================

    private void AssignSpawnPoint()
    {
        if (SpawnPointsManager.Instance == null)
        {
            Debug.LogError(
                "RespawnManager: " +
                "SpawnPointsManager not found!"
            );

            return;
        }

        Transform spawnPoint =
            SpawnPointsManager.Instance.GetSpawnPoint(
                OwnerClientId
            );

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"RespawnManager: " +
                $"Could not find spawn point for " +
                $"Client {OwnerClientId}."
            );

            return;
        }

        spawnPosition =
            spawnPoint.position;

        spawnRotation =
            spawnPoint.rotation;

        Debug.Log(
            $"Player {OwnerClientId} assigned to " +
            $"{spawnPoint.name}."
        );
    }

    // =========================================================
    // INITIAL SPAWN
    // =========================================================

    private void SendInitialSpawnToOwner()
    {
        SendInitialSpawnToOwnerClientRpc(
            spawnPosition,
            spawnRotation
        );
    }

    [ClientRpc]
    private void SendInitialSpawnToOwnerClientRpc(
        Vector3 position,
        Quaternion rotation)
    {
        if (!IsOwner)
            return;

        PerformLocalSpawn(
            position,
            rotation
        );
    }

    // =========================================================
    // DEADLINE COLLISION
    // =========================================================

    private void OnControllerColliderHit(
        ControllerColliderHit hit)
    {
        if (SceneManager.GetActiveScene().name !=
            gameSceneName)
        {
            return;
        }

        if (!IsOwner)
            return;

        if (!hit.collider.CompareTag("Deadline"))
            return;

        if (IsDead.Value ||
            deathRequested ||
            DeathTriggeredLocally)
        {
            return;
        }

        deathRequested = true;

        // Immediately stop local player control.
        EnterLocalDeathState();

        RequestDeathServerRpc();
    }

    // =========================================================
    // LOCAL DEATH
    // =========================================================

    private void EnterLocalDeathState()
    {
        if (DeathTriggeredLocally)
            return;

        DeathTriggeredLocally = true;

        // -----------------------------------------------------
        // STOP PLAYER MOVEMENT
        // -----------------------------------------------------

        PlayerMovement playerMovement =
            GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.ResetVelocity();

            playerMovement.SetMovementControlEnabled(
                false
            );
        }

        // -----------------------------------------------------
        // STOP PLAYER LOOK
        // -----------------------------------------------------

        PlayerLookManager playerLook =
            GetComponent<PlayerLookManager>();

        if (playerLook != null)
        {
            playerLook.ShowPlayerModelForDeath();

            playerLook.enabled = false;
        }

        // -----------------------------------------------------
        // FORCE SPECTATOR CAMERA
        // -----------------------------------------------------

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.EnterSpectatorMode(
                transform
            );
        }

        // -----------------------------------------------------
        // PLAY DEATH ANIMATION
        // -----------------------------------------------------

        PlayerAnimationManager animationManager =
            GetComponent<PlayerAnimationManager>();

        if (animationManager != null)
        {
            animationManager.SetDeathAnimation();
        }
    }

    // =========================================================
    // REQUEST DEATH
    // =========================================================

    [ServerRpc]
    private void RequestDeathServerRpc()
    {
        if (!gameplayInitialized)
        {
            deathRequested = false;
            return;
        }

        if (IsDead.Value)
            return;

        IsDead.Value = true;

        // Notify the round manager.
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.RegisterPlayerDeath(
                OwnerClientId
            );
        }

        PlayDeathSoundClientRpc();
    }

    // =========================================================
    // DEATH STATE CHANGED
    // =========================================================

    private void OnDeathStateChanged(
        bool previousState,
        bool newState)
    {
        if (!newState)
            return;

        // Owner already enters death state immediately
        // when touching the Deadline.
        if (IsOwner)
        {
            EnterLocalDeathState();
        }

        // Remote players need the death animation.
        if (!IsOwner)
        {
            PlayerAnimationManager animationManager =
                GetComponent<PlayerAnimationManager>();

            if (animationManager != null)
            {
                animationManager.SetDeathAnimation();
            }
        }
    }

    // =========================================================
    // DEATH SOUND
    // =========================================================

    [ClientRpc]
    private void PlayDeathSoundClientRpc()
    {
        if (deathAudioSource == null)
            return;

        AudioClip selectedDeathSound = null;

        // Use the player's saved Very Funny Mode setting.
        if (GameSettingsManager.Instance != null &&
            GameSettingsManager.Instance.VeryFunnyMode)
        {
            selectedDeathSound =
                funnyDeathSound;
        }
        else
        {
            selectedDeathSound =
                normalDeathSound;
        }

        if (selectedDeathSound == null)
        {
            Debug.LogWarning(
                "RespawnManager: " +
                "Selected death sound is not assigned."
            );

            return;
        }

        deathAudioSource.PlayOneShot(
            selectedDeathSound
        );
    }

    // =========================================================
    // NEW ROUND RESET
    // =========================================================

    public void ResetForNewRound(
        Vector3 position,
        Quaternion rotation)
    {
        if (!IsServer)
            return;

        IsDead.Value = false;

        ResetForNewRoundClientRpc(
            position,
            rotation
        );
    }

    [ClientRpc]
    private void ResetForNewRoundClientRpc(
        Vector3 position,
        Quaternion rotation)
    {
        if (!IsOwner)
            return;

        PerformLocalSpawn(
            position,
            rotation
        );
    }

    // =========================================================
    // PERFORM LOCAL SPAWN
    // =========================================================

    private void PerformLocalSpawn(
        Vector3 position,
        Quaternion rotation)
    {
        if (characterController == null)
            return;

        // -----------------------------------------------------
        // RESET LOCAL DEATH STATE
        // -----------------------------------------------------

        DeathTriggeredLocally = false;
        deathRequested = false;

        // -----------------------------------------------------
        // DISABLE CHARACTER CONTROLLER DURING TELEPORT
        // -----------------------------------------------------

        characterController.enabled = false;

        transform.SetPositionAndRotation(
            position,
            rotation
        );

        // -----------------------------------------------------
        // RESET PLAYER MODEL
        // -----------------------------------------------------

        if (playerModel != null)
        {
            playerModel.localPosition =
                modelLocalPosition;

            playerModel.localRotation =
                modelLocalRotation;
        }

        // -----------------------------------------------------
        // RESET MOVEMENT
        // -----------------------------------------------------

        PlayerMovement playerMovement =
            GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.ResetVelocity();

            playerMovement.SetMovementControlEnabled(
                true
            );
        }

        // -----------------------------------------------------
        // RESET LOOK
        // -----------------------------------------------------

        PlayerLookManager playerLook =
            GetComponent<PlayerLookManager>();

        if (playerLook != null)
        {
            playerLook.ResetLook(rotation);

            playerLook.enabled = true;
        }

        // -----------------------------------------------------
        // RESET ANIMATION
        // -----------------------------------------------------

        PlayerAnimationManager animationManager =
            GetComponent<PlayerAnimationManager>();

        if (animationManager != null)
        {
            animationManager.SetAliveAnimation();
        }

        // -----------------------------------------------------
        // RESET CAMERA
        // -----------------------------------------------------

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.ResetForNewRound(
                transform,
                rotation
            );
        }

        // -----------------------------------------------------
        // RESET MODEL VISIBILITY
        // -----------------------------------------------------

        if (playerLook != null)
        {
            playerLook.RefreshPlayerModelVisibility();
        }

        characterController.enabled = true;

        Debug.Log(
            $"Player {OwnerClientId} " +
            $"reset for new round at {position}."
        );
    }

    // =========================================================
    // PUBLIC INFORMATION
    // =========================================================

    public Vector3 GetSpawnPosition()
    {
        return spawnPosition;
    }

    public Quaternion GetSpawnRotation()
    {
        return spawnRotation;
    }
}