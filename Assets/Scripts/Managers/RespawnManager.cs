using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class RespawnManager : MonoBehaviour
{
    [Header("Deadline")]
    [SerializeField] private Collider deadlineCollider;

    [Header("Player Model")]
    [SerializeField] private Transform playerModel;

    private Vector3 modelLocalPosition;
    private Quaternion modelLocalRotation;

    private CharacterController characterController;

    private Vector3 respawnPosition;
    private Quaternion respawnRotation;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Save the player's starting position.
        respawnPosition = transform.position;
        respawnRotation = transform.rotation;

        if (playerModel != null)
        {
            modelLocalPosition = playerModel.localPosition;
            modelLocalRotation = playerModel.localRotation;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (deadlineCollider == null)
            return;

        if (hit.collider != deadlineCollider)
            return;

        Respawn();
    }

    public void Respawn()
    {
        // Disable CharacterController before teleporting.
        characterController.enabled = false;

        // Reset Player1 position and rotation.
        transform.position = respawnPosition;
        transform.rotation = respawnRotation;

        // Reset SWAT model position and rotation.
        if (playerModel != null)
        {
            playerModel.localPosition = modelLocalPosition;
            playerModel.localRotation = modelLocalRotation;
        }

        // Reset movement velocity.
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.ResetVelocity();
        }

        // Re-enable CharacterController.
        characterController.enabled = true;
    }
}