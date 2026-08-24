using UnityEngine;

public class PlayerAnimationManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Animation Settings")]
    [SerializeField] private float animationTransitionTime = 0.1f;

    private string currentAnimation;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    private void LateUpdate()
    {
        if (animator == null || playerMovement == null)
            return;

        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        // ==========================================
        // AIR STATE
        // ==========================================

        if (playerMovement.CurrentState == PlayerMovement.MovementState.Air)
        {
            HandleAirAnimation();
            return;
        }

        // ==========================================
        // GROUND STATE
        // ==========================================

        HandleGroundAnimation();
    }

    private void HandleGroundAnimation()
    {
        Vector2 input = InputManager.Instance != null
            ? InputManager.Instance.MoveInput
            : Vector2.zero;

        // No movement input = Idle
        if (input.sqrMagnitude <= 0.001f)
        {
            PlayAnimation("Idle");
            return;
        }

        // W
        if (input.y > 0.1f)
        {
            PlayAnimation("Running_Front");
            return;
        }

        // S
        if (input.y < -0.1f)
        {
            PlayAnimation("Running_Back");
            return;
        }

        // A
        if (input.x < -0.1f)
        {
            PlayAnimation("Running_Left");
            return;
        }

        // D
        if (input.x > 0.1f)
        {
            PlayAnimation("Running_Right");
            return;
        }

        PlayAnimation("Idle");
    }

    private void HandleAirAnimation()
    {
        float verticalVelocity = playerMovement.Velocity.y;

        // Moving upward
        if (verticalVelocity > 0.1f)
        {
            PlayAnimation("Jumping_Up");
            return;
        }

        // Moving downward
        if (verticalVelocity < -0.1f)
        {
            PlayAnimation("Jumping_Down");
            return;
        }

        // At the peak of the jump.
        // Keep the previous jump animation instead
        // of switching to Idle.
    }

    private void PlayAnimation(string animationName)
    {
        if (currentAnimation == animationName)
            return;

        currentAnimation = animationName;

        animator.CrossFade(
            animationName,
            animationTransitionTime
        );
    }
}