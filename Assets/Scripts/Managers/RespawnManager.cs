using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class RespawnManager : MonoBehaviour
{
    [Header("Deadline")]
    [SerializeField] private Collider deadlineCollider;

    private CharacterController characterController;

    private Vector3 respawnPosition;
    private Quaternion respawnRotation;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Save the player's starting position.
        respawnPosition = transform.position;
        respawnRotation = transform.rotation;
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

        transform.position = respawnPosition;
        transform.rotation = respawnRotation;

        characterController.enabled = true;

        // Reset movement velocity.
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.ResetVelocity();
        }
    }
}