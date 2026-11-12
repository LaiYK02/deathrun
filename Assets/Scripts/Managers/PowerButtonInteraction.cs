using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(RoundManager))]
public class PowerButtonInteraction : NetworkBehaviour
{
    private const float InteractionRange = 2.5f;
    private const float MaxFacingAngle = 15f;

    [SerializeField] private SpriteRenderer buttonRenderer;
    [SerializeField] private Sprite usedSprite;

    private readonly NetworkVariable<Activation> activation = new(new Activation(-1, 0d));
    private RoundManager roundManager;
    private Sprite availableSprite;

    internal readonly struct Activation : INetworkSerializeByMemcpy
    {
        public readonly int Round;
        public readonly double ServerTime;

        public Activation(int round, double serverTime)
        {
            Round = round;
            ServerTime = serverTime;
        }
    }

    private void Awake()
    {
        roundManager = GetComponent<RoundManager>();
        availableSprite = buttonRenderer.sprite;
    }

    public override void OnNetworkSpawn()
    {
        activation.OnValueChanged += OnActivationChanged;
        roundManager.CurrentRound.OnValueChanged += OnRoundChanged;
        RefreshSprite();
    }

    public override void OnNetworkDespawn()
    {
        activation.OnValueChanged -= OnActivationChanged;
        roundManager.CurrentRound.OnValueChanged -= OnRoundChanged;
        buttonRenderer.sprite = availableSprite;
    }

    public bool TryGetActivePressElapsedTime(out double elapsedSeconds)
    {
        elapsedSeconds = 0d;
        if (!IsSpawned || !roundManager.IsRoundActive || activation.Value.Round != roundManager.CurrentRound.Value)
            return false;

        elapsedSeconds = NetworkManager.ServerTime.Time - activation.Value.ServerTime;
        return true;
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsClient || InputManager.Instance == null ||
            !InputManager.Instance.InteractPressed ||
            (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused))
        {
            return;
        }

        int currentRound = roundManager.CurrentRound.Value;
        NetworkObject player = NetworkManager.LocalClient?.PlayerObject;
        if (!CanActivate(currentRound, player))
            return;

        Camera view = Camera.main;
        Vector3 buttonPosition = buttonRenderer.transform.position;
        if (view == null || !IsFacingButton(view.transform.position, view.transform.forward, buttonPosition))
            return;

        ActivateRpc(currentRound);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ActivateRpc(int requestedRound, RpcParams rpcParams = default)
    {
        if (NetworkManager.ConnectedClients.TryGetValue(rpcParams.Receive.SenderClientId, out var sender) &&
            CanActivate(requestedRound, sender.PlayerObject))
        {
            activation.Value = new Activation(requestedRound, NetworkManager.ServerTime.Time);
        }
    }

    private bool CanActivate(int requestedRound, NetworkObject player)
    {
        if (!roundManager.IsRoundActive || requestedRound != roundManager.CurrentRound.Value ||
            activation.Value.Round == requestedRound || player == null || !player.IsSpawned ||
            !player.TryGetComponent(out RespawnManager respawn) || respawn.IsDead.Value || respawn.DeathTriggeredLocally)
        {
            return false;
        }

        return IsWithinRange(player.transform.position, buttonRenderer.transform.position);
    }

    private static bool IsWithinRange(Vector3 playerPosition, Vector3 buttonPosition)
    {
        return (buttonPosition - playerPosition).sqrMagnitude <= InteractionRange * InteractionRange;
    }

    private static bool IsFacingButton(Vector3 viewPosition, Vector3 viewForward, Vector3 buttonPosition)
    {
        float angle = Vector3.Angle(viewForward, buttonPosition - viewPosition);
        return angle <= MaxFacingAngle || Mathf.Approximately(angle, MaxFacingAngle);
    }

    private void OnActivationChanged(Activation previousValue, Activation newValue)
    {
        RefreshSprite();
    }

    private void OnRoundChanged(int previousValue, int newValue)
    {
        RefreshSprite();
    }

    private void RefreshSprite()
    {
        buttonRenderer.sprite = activation.Value.Round == roundManager.CurrentRound.Value
            ? usedSprite
            : availableSprite;
    }
}
