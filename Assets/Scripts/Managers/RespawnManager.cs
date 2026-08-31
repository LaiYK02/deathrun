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

    // Stored by the server.
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

        // The server records the initial spawn position.
        if (IsServer)
        {
            respawnPosition = transform.position;
            respawnRotation = transform.rotation;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Only the local player should request its own respawn.
        if (!IsOwner)
            return;

        // Only react to the Deadline.
        if (!hit.collider.CompareTag("Deadline"))
            return;

        // Prevent multiple requests while touching the Deadline.
        if (respawnRequested)
            return;

        respawnRequested = true;

        RequestRespawnServerRpc();
    }

    [ServerRpc]
    private void RequestRespawnServerRpc()
    {
        // Server decides whether this player should respawn.
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
        // Only the owner should perform the actual teleport.
        if (!IsOwner)
            return;

        PerformLocalRespawn(position, rotation);
    }

    private void PerformLocalRespawn(
        Vector3 position,
        Quaternion rotation)
    {
        // Disable CharacterController before teleporting.
        characterController.enabled = false;

        // Move the player's own authoritative Transform.
        transform.position = position;
        transform.rotation = rotation;

        // Reset model.
        if (playerModel != null)
        {
            playerModel.localPosition = modelLocalPosition;
            playerModel.localRotation = modelLocalRotation;
        }

        // Reset movement velocity.
        PlayerMovement playerMovement =
            GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.ResetVelocity();
        }

        // Re-enable CharacterController.
        characterController.enabled = true;

        respawnRequested = false;
    }

    public void Respawn()
    {
        // For other scripts that may manually request a respawn.
        if (!IsOwner)
            return;

        if (IsServer)
        {
            PerformLocalRespawn(
                respawnPosition,
                respawnRotation
            );
        }
        else
        {
            RequestRespawnServerRpc();
        }
    }
}