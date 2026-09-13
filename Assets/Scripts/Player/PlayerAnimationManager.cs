using UnityEngine;
using Unity.Netcode;

public class PlayerAnimationManager : NetworkBehaviour
{
    // =========================================================
    // ANIMATION STATES
    // =========================================================

    private enum AnimationState
    {
        Idle = 0,
        RunningFront = 1,
        RunningBack = 2,
        RunningLeft = 3,
        RunningRight = 4,
        JumpingUp = 5,
        JumpingDown = 6,
        Death = 7
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Animation Settings")]
    [SerializeField] private float animationTransitionTime = 0.1f;

    private RespawnManager respawnManager;

    // =========================================================
    // NETWORK STATE
    // =========================================================

    private NetworkVariable<AnimationState> networkAnimationState =
        new NetworkVariable<AnimationState>(
            AnimationState.Idle,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

    private AnimationState currentAnimationState =
        AnimationState.Idle;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }

        respawnManager =
            GetComponent<RespawnManager>();
    }

    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        networkAnimationState.OnValueChanged +=
            OnAnimationStateChanged;

        PlayAnimation(
            networkAnimationState.Value
        );
    }

    // =========================================================
    // NETWORK DESPAWN
    // =========================================================

    public override void OnNetworkDespawn()
    {
        networkAnimationState.OnValueChanged -=
            OnAnimationStateChanged;

        base.OnNetworkDespawn();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void LateUpdate()
    {
        if (!IsSpawned)
            return;

        if (!IsOwner)
            return;

        UpdateLocalAnimation();
    }

    // =========================================================
    // LOCAL ANIMATION
    // =========================================================

    private void UpdateLocalAnimation()
    {
        if (animator == null ||
            playerMovement == null)
        {
            return;
        }

        // -----------------------------------------------------
        // DEAD
        // -----------------------------------------------------

        if (respawnManager != null &&
            (respawnManager.IsDead.Value ||
             respawnManager.DeathTriggeredLocally))
        {
            SetAnimationState(
                AnimationState.Death
            );

            return;
        }

        AnimationState newState;

        // -----------------------------------------------------
        // AIR
        // -----------------------------------------------------

        if (playerMovement.CurrentState ==
            PlayerMovement.MovementState.Air)
        {
            float verticalVelocity =
                playerMovement.Velocity.y;

            if (verticalVelocity > 0.1f)
            {
                newState =
                    AnimationState.JumpingUp;
            }
            else if (verticalVelocity < -0.1f)
            {
                newState =
                    AnimationState.JumpingDown;
            }
            else
            {
                return;
            }
        }

        // -----------------------------------------------------
        // GROUND
        // -----------------------------------------------------

        else
        {
            Vector2 input =
                InputManager.Instance != null
                    ? InputManager.Instance.MoveInput
                    : Vector2.zero;

            if (input.sqrMagnitude <= 0.001f)
            {
                newState =
                    AnimationState.Idle;
            }
            else if (input.y > 0.1f)
            {
                newState =
                    AnimationState.RunningFront;
            }
            else if (input.y < -0.1f)
            {
                newState =
                    AnimationState.RunningBack;
            }
            else if (input.x < -0.1f)
            {
                newState =
                    AnimationState.RunningLeft;
            }
            else
            {
                newState =
                    AnimationState.RunningRight;
            }
        }

        SetAnimationState(newState);
    }

    // =========================================================
    // SET ANIMATION STATE
    // =========================================================

    private void SetAnimationState(
        AnimationState newState)
    {
        if (networkAnimationState.Value ==
            newState)
        {
            return;
        }

        networkAnimationState.Value =
            newState;

        PlayAnimation(newState);
    }

    // =========================================================
    // DEATH ANIMATION
    // =========================================================

    public void SetDeathAnimation()
    {
        if (!IsSpawned)
            return;

        if (!IsOwner)
        {
            PlayAnimation(
                AnimationState.Death
            );

            return;
        }

        SetAnimationState(
            AnimationState.Death
        );
    }

    // =========================================================
    // ALIVE ANIMATION
    // =========================================================

    public void SetAliveAnimation()
    {
        if (!IsSpawned)
            return;

        if (!IsOwner)
        {
            PlayAnimation(
                AnimationState.Idle
            );

            return;
        }

        SetAnimationState(
            AnimationState.Idle
        );
    }

    // =========================================================
    // NETWORK CALLBACK
    // =========================================================

    private void OnAnimationStateChanged(
        AnimationState previousState,
        AnimationState newState)
    {
        PlayAnimation(newState);
    }

    // =========================================================
    // PLAY ANIMATION
    // =========================================================

    private void PlayAnimation(
        AnimationState state)
    {
        if (animator == null)
            return;

        if (currentAnimationState == state)
            return;

        currentAnimationState =
            state;

        string animationName =
            GetAnimationName(state);

        animator.CrossFade(
            animationName,
            animationTransitionTime
        );
    }

    // =========================================================
    // ANIMATION NAME
    // =========================================================

    private string GetAnimationName(
        AnimationState state)
    {
        switch (state)
        {
            case AnimationState.RunningFront:
                return "Running_Front";

            case AnimationState.RunningBack:
                return "Running_Back";

            case AnimationState.RunningLeft:
                return "Running_Left";

            case AnimationState.RunningRight:
                return "Running_Right";

            case AnimationState.JumpingUp:
                return "Jumping_Up";

            case AnimationState.JumpingDown:
                return "Jumping_Down";

            case AnimationState.Death:
                return "Death";

            case AnimationState.Idle:
            default:
                return "Idle";
        }
    }
}