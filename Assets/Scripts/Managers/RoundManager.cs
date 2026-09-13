using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Round Settings")]
    [SerializeField] private float nextRoundDelay = 5f;

    public NetworkVariable<int> CurrentRound =
        new NetworkVariable<int>(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly HashSet<ulong> deadPlayers =
        new HashSet<ulong>();

    private Coroutine nextRoundCoroutine;

    private bool roundEnding;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            NetworkManager.Singleton
                .OnClientDisconnectCallback +=
                OnClientDisconnected;
        }

        Debug.Log(
            $"RoundManager: Round " +
            $"{CurrentRound.Value} started."
        );
    }

    // =========================================================
    // NETWORK DESPAWN
    // =========================================================

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null &&
            IsServer)
        {
            NetworkManager.Singleton
                .OnClientDisconnectCallback -=
                OnClientDisconnected;
        }

        if (nextRoundCoroutine != null)
        {
            StopCoroutine(
                nextRoundCoroutine
            );

            nextRoundCoroutine = null;
        }

        base.OnNetworkDespawn();
    }

    // =========================================================
    // PLAYER DEATH
    // =========================================================

    public void RegisterPlayerDeath(
        ulong clientId)
    {
        if (!IsServer)
            return;

        if (roundEnding)
            return;

        if (deadPlayers.Contains(clientId))
            return;

        deadPlayers.Add(clientId);

        Debug.Log(
            $"RoundManager: Client {clientId} died. " +
            $"Dead players = " +
            $"{deadPlayers.Count}/" +
            $"{NetworkManager.Singleton.ConnectedClientsIds.Count}"
        );

        if (AllPlayersDead())
        {
            StartNextRoundCountdown();
        }
    }

    // =========================================================
    // ALL PLAYERS DEAD
    // =========================================================

    private bool AllPlayersDead()
    {
        if (NetworkManager.Singleton == null)
            return false;

        int playerCount =
            NetworkManager.Singleton
                .ConnectedClientsIds
                .Count;

        if (playerCount <= 0)
            return false;

        foreach (ulong clientId in
                 NetworkManager.Singleton
                     .ConnectedClientsIds)
        {
            if (!deadPlayers.Contains(clientId))
            {
                return false;
            }
        }

        return true;
    }

    // =========================================================
    // START COUNTDOWN
    // =========================================================

    private void StartNextRoundCountdown()
    {
        if (roundEnding)
            return;

        roundEnding = true;

        nextRoundCoroutine =
            StartCoroutine(
                NextRoundCountdown()
            );
    }

    private IEnumerator NextRoundCountdown()
    {
        Debug.Log(
            $"RoundManager: All players dead. " +
            $"Next round in {nextRoundDelay} seconds."
        );

        yield return new WaitForSeconds(
            nextRoundDelay
        );

        // A player could disconnect during the
        // countdown.
        if (!AllPlayersDead())
        {
            roundEnding = false;
            nextRoundCoroutine = null;

            yield break;
        }

        StartNewRound();

        nextRoundCoroutine = null;
    }

    // =========================================================
    // START NEW ROUND
    // =========================================================

    private void StartNewRound()
    {
        if (!IsServer)
            return;

        CurrentRound.Value++;

        Debug.Log(
            $"RoundManager: Starting Round " +
            $"{CurrentRound.Value}."
        );

        deadPlayers.Clear();

        roundEnding = false;

        // -----------------------------------------------------
        // RESET EVERY PLAYER
        // -----------------------------------------------------

        foreach (ulong clientId in
                 NetworkManager.Singleton
                     .ConnectedClientsIds)
        {
            NetworkClient client =
                NetworkManager.Singleton
                    .ConnectedClients[clientId];

            if (client.PlayerObject == null)
                continue;

            RespawnManager respawnManager =
                client.PlayerObject
                    .GetComponent<RespawnManager>();

            if (respawnManager == null)
                continue;

            if (SpawnPointsManager.Instance == null)
                continue;

            Transform spawnPoint =
                SpawnPointsManager.Instance
                    .GetSpawnPoint(clientId);

            if (spawnPoint == null)
                continue;

            respawnManager.ResetForNewRound(
                spawnPoint.position,
                spawnPoint.rotation
            );
        }

        Debug.Log(
            $"RoundManager: Round " +
            $"{CurrentRound.Value} is now active."
        );
    }

    // =========================================================
    // PLAYER DISCONNECT
    // =========================================================

    private void OnClientDisconnected(
        ulong clientId)
    {
        if (!IsServer)
            return;

        deadPlayers.Remove(clientId);

        // If someone disconnects during the
        // 5-second countdown, check again.
        if (roundEnding &&
            !AllPlayersDead())
        {
            if (nextRoundCoroutine != null)
            {
                StopCoroutine(
                    nextRoundCoroutine
                );

                nextRoundCoroutine = null;
            }

            roundEnding = false;

            Debug.Log(
                "RoundManager: " +
                "Next-round countdown cancelled."
            );
        }
    }
}