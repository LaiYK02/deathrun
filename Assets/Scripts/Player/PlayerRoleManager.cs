using Unity.Netcode;
using UnityEngine;

public enum PlayerRole
{
    Runner,
    Trapper
}

public class PlayerRoleManager : NetworkBehaviour
{
    [Header("Default Role")]
    [SerializeField]
    private PlayerRole defaultRole =
        PlayerRole.Runner;

    public NetworkVariable<PlayerRole> Role =
        new NetworkVariable<PlayerRole>(
            PlayerRole.Runner,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public bool IsRunner =>
        Role.Value == PlayerRole.Runner;

    public bool IsTrapper =>
        Role.Value == PlayerRole.Trapper;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            Role.Value = defaultRole;
        }
    }
}