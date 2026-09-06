using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class LobbyHeartbeatManager : NetworkBehaviour
{
    [Header("Heartbeat Settings")]
    [SerializeField] private float heartbeatInterval = 0.5f;

    private Coroutine heartbeatCoroutine;

    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only the host/server sends the heartbeat.
        if (IsServer)
        {
            StartHeartbeat();
        }
    }

    // =========================================================
    // NETWORK DESPAWN
    // =========================================================

    public override void OnNetworkDespawn()
    {
        StopHeartbeat();

        base.OnNetworkDespawn();
    }

    // =========================================================
    // START HEARTBEAT
    // =========================================================

    private void StartHeartbeat()
    {
        StopHeartbeat();

        heartbeatCoroutine =
            StartCoroutine(HeartbeatCoroutine());
    }

    // =========================================================
    // STOP HEARTBEAT
    // =========================================================

    private void StopHeartbeat()
    {
        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = null;
        }
    }

    // =========================================================
    // HEARTBEAT
    // =========================================================

    private IEnumerator HeartbeatCoroutine()
    {
        while (IsServer &&
               NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening)
        {
            // Tell all connected clients to refresh
            // their lobby player list.
            RefreshPlayerListClientRpc();

            yield return new WaitForSeconds(
                heartbeatInterval
            );
        }

        heartbeatCoroutine = null;
    }

    // =========================================================
    // CLIENT RPC
    // =========================================================

    [ClientRpc]
    private void RefreshPlayerListClientRpc()
    {
        LobbyManager lobbyManager =
            FindFirstObjectByType<LobbyManager>();

        if (lobbyManager == null)
            return;

        lobbyManager.RefreshPlayerListFromHeartbeat();
    }
}