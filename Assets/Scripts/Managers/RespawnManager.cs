using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class RespawnManager : NetworkBehaviour
{
    [Header("Player Model")]
    [SerializeField] private Transform playerModel;

    [Header("Gameplay Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    private Vector3 modelLocalPosition;
    private Quaternion modelLocalRotation;

    private CharacterController characterController;

    private Vector3 respawnPosition;
    private Quaternion respawnRotation;

    private bool respawnRequested;
    private bool gameplayInitialized;
    private Coroutine initializeCoroutine;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (playerModel != null)
        {
            modelLocalPosition = playerModel.localPosition;
            modelLocalRotation = playerModel.localRotation;
        }
    }

    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Listen for scene changes.
        SceneManager.sceneLoaded += OnSceneLoaded;

        // If the player was spawned directly in GameScene,
        // initialize immediately.
        if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            StartGameplayInitialization();
        }
    }

    // =========================================================
    // NETWORK DESPAWN
    // =========================================================

    public override void OnNetworkDespawn()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

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
        // Ignore Lobby and other scenes.
        if (scene.name != gameSceneName)
            return;

        StartGameplayInitialization();
    }

    // =========================================================
    // START GAMEPLAY INITIALIZATION
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

    // =========================================================
    // INITIALIZE GAMEPLAY
    // =========================================================

    private IEnumerator InitializeGameplayAfterSceneLoad()
    {
        // Wait one frame so GameScene objects have time
        // to initialize their Awake() methods.
        yield return null;

        // Wait until SpawnPointsManager exists.
        while (SpawnPointsManager.Instance == null)
        {
            yield return null;
        }

        initializeCoroutine = null;

        if (gameplayInitialized)
            yield break;

        gameplayInitialized = true;

        // Only the server assigns the network player's
        // respawn position.
        if (IsServer)
        {
            AssignRespawnPoint();

            SendInitialSpawnToOwner();
        }
    }

    // =========================================================
    // ASSIGN RESPAWN POINT
    // =========================================================

    private void AssignRespawnPoint()
    {
        if (SpawnPointsManager.Instance == null)
        {
            Debug.LogError(
                "RespawnManager: SpawnPointsManager not found!"
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
                $"RespawnManager: Could not find spawn point " +
                $"for Client {OwnerClientId}."
            );

            return;
        }

        respawnPosition = spawnPoint.position;
        respawnRotation = spawnPoint.rotation;

        Debug.Log(
            $"Player {OwnerClientId} assigned to " +
            $"{spawnPoint.name} at {spawnPoint.position}"
        );
    }

    // =========================================================
    // SEND INITIAL SPAWN
    // =========================================================

    private void SendInitialSpawnToOwner()
    {
        SendInitialSpawnToOwnerClientRpc(
            respawnPosition,
            respawnRotation
        );
    }

    [ClientRpc]
    private void SendInitialSpawnToOwnerClientRpc(
        Vector3 position,
        Quaternion rotation)
    {
        if (!IsOwner)
            return;

        PerformLocalRespawn(
            position,
            rotation,
            true
        );
    }

    // =========================================================
    // DEADLINE COLLISION
    // =========================================================

    private void OnControllerColliderHit(
        ControllerColliderHit hit)
    {
        // Do not process respawning in Lobby.
        if (SceneManager.GetActiveScene().name != gameSceneName)
            return;

        if (!IsOwner)
            return;

        if (!hit.collider.CompareTag("Deadline"))
            return;

        if (respawnRequested)
            return;

        respawnRequested = true;

        RequestRespawnServerRpc();
    }

    // =========================================================
    // REQUEST RESPAWN
    // =========================================================

    [ServerRpc]
    private void RequestRespawnServerRpc()
    {
        if (!gameplayInitialized)
        {
            respawnRequested = false;
            return;
        }

        if (SpawnPointsManager.Instance == null)
        {
            Debug.LogError(
                "RespawnManager: SpawnPointsManager not found!"
            );

            respawnRequested = false;
            return;
        }

        Transform spawnPoint =
            SpawnPointsManager.Instance.GetSpawnPoint(
                OwnerClientId
            );

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"RespawnManager: No spawn point for " +
                $"Client {OwnerClientId}."
            );

            respawnRequested = false;
            return;
        }

        respawnPosition = spawnPoint.position;
        respawnRotation = spawnPoint.rotation;

        Debug.Log(
            $"Respawning Client {OwnerClientId} at " +
            $"{spawnPoint.name}"
        );

        SendRespawnToOwnerClientRpc(
            respawnPosition,
            respawnRotation
        );
    }

    // =========================================================
    // SEND RESPAWN
    // =========================================================

    [ClientRpc]
    private void SendRespawnToOwnerClientRpc(
        Vector3 position,
        Quaternion rotation)
    {
        if (!IsOwner)
            return;

        PerformLocalRespawn(
            position,
            rotation,
            false
        );
    }

    // =========================================================
    // PERFORM LOCAL RESPAWN
    // =========================================================

    private void PerformLocalRespawn(
        Vector3 position,
        Quaternion rotation,
        bool initialSpawn)
    {
        if (characterController == null)
            return;

        // Prevent CharacterController from fighting
        // against the teleport.
        characterController.enabled = false;

        // Reset player position and direction.
        transform.SetPositionAndRotation(
            position,
            rotation
        );

        // Reset model.
        if (playerModel != null)
        {
            playerModel.localPosition =
                modelLocalPosition;

            playerModel.localRotation =
                modelLocalRotation;
        }

        // Reset movement.
        PlayerMovement playerMovement =
            GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.ResetVelocity();
        }

        // Reset player look.
        PlayerLookManager playerLookManager =
            GetComponent<PlayerLookManager>();

        if (playerLookManager != null)
        {
            playerLookManager.ResetLook(rotation);
        }

        // Reset third-person camera.
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.ResetThirdPersonCamera(
                transform,
                rotation
            );
        }

        characterController.enabled = true;

        respawnRequested = false;

        Debug.Log(
            initialSpawn
                ? $"Initial spawn at {position}"
                : $"Respawned at {position}"
        );
    }

    // =========================================================
    // PUBLIC RESPAWN
    // =========================================================

    public void Respawn()
    {
        // Respawn only in GameScene.
        if (SceneManager.GetActiveScene().name != gameSceneName)
            return;

        if (!IsOwner)
            return;

        if (!gameplayInitialized)
            return;

        if (SpawnPointsManager.Instance == null)
            return;

        if (IsServer)
        {
            Transform spawnPoint =
                SpawnPointsManager.Instance.GetSpawnPoint(
                    OwnerClientId
                );

            if (spawnPoint == null)
                return;

            respawnPosition = spawnPoint.position;
            respawnRotation = spawnPoint.rotation;

            PerformLocalRespawn(
                respawnPosition,
                respawnRotation,
                false
            );
        }
        else
        {
            RequestRespawnServerRpc();
        }
    }
}