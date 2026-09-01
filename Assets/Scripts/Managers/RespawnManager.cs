using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class RespawnManager : NetworkBehaviour
{
    [Header("Player Model")]
    [SerializeField] private Transform playerModel;

    private Vector3 modelLocalPosition;
    private Quaternion modelLocalRotation;

    private CharacterController characterController;

    private Vector3 respawnPosition;
    private Quaternion respawnRotation;

    private bool respawnRequested;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (playerModel != null)
        {
            modelLocalPosition = playerModel.localPosition;
            modelLocalRotation = playerModel.localRotation;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            AssignRespawnPoint();
        }

        // Wait until the server has assigned the spawn point,
        // then send the correct position to the owner.
        if (IsServer)
        {
            SendInitialSpawnToOwner();
        }
    }

    private void AssignRespawnPoint()
    {
        if (SpawnPointsManager.Instance == null)
        {
            Debug.LogError(
                "RespawnManager: SpawnPointsManager not found!"
            );

            respawnPosition = transform.position;
            respawnRotation = transform.rotation;

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

            respawnPosition = transform.position;
            respawnRotation = transform.rotation;

            return;
        }

        respawnPosition = spawnPoint.position;
        respawnRotation = spawnPoint.rotation;

        Debug.Log(
            $"Player {OwnerClientId} assigned to " +
            $"{spawnPoint.name} at {spawnPoint.position}"
        );
    }

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

    private void OnControllerColliderHit(
        ControllerColliderHit hit)
    {
        if (!IsOwner)
            return;

        if (!hit.collider.CompareTag("Deadline"))
            return;

        if (respawnRequested)
            return;

        respawnRequested = true;

        RequestRespawnServerRpc();
    }

    [ServerRpc]
    private void RequestRespawnServerRpc()
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
                $"RespawnManager: No spawn point for " +
                $"Client {OwnerClientId}."
            );

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

    private void PerformLocalRespawn(
        Vector3 position,
        Quaternion rotation,
        bool initialSpawn)
    {
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

    public void Respawn()
    {
        if (!IsOwner)
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