using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum RoundPhase
{
    Warmup,
    Active,
    Ending
}

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    public const ulong NoTrapper = ulong.MaxValue;

    [Header("Warmup")]
    [SerializeField] private float warmupDuration = 30f;
    [SerializeField] private float trapperSelectionTime = 5f;

    [Header("Round")]
    [SerializeField] private float roundDuration = 300f;
    [SerializeField] private float nextRoundDelay = 5f;

    [Header("Player Prefabs")]
    [SerializeField] private GameObject runnerPrefab;
    [SerializeField] private GameObject trapperPrefab;

    // =========================================================
    // NETWORK VARIABLES
    // =========================================================

    public NetworkVariable<int> CurrentRound =
        new NetworkVariable<int>(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<RoundPhase> Phase =
        new NetworkVariable<RoundPhase>(
            RoundPhase.Warmup,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<int> PhaseTimeRemaining =
        new NetworkVariable<int>(
            30,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<ulong> TrapperClientId =
        new NetworkVariable<ulong>(
            NoTrapper,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public bool IsRoundActive =>
        Phase.Value == RoundPhase.Active;

    // =========================================================
    // PRIVATE STATE
    // =========================================================

    private readonly HashSet<ulong> deadPlayers =
        new HashSet<ulong>();

    private Coroutine roundFlowCoroutine;

    private bool roundEndingBecauseTrapperDied;
    private bool roundEndingBecauseTimeExpired;

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

            roundFlowCoroutine =
                StartCoroutine(
                    StartGameFlow()
                );
        }
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

        if (roundFlowCoroutine != null)
        {
            StopCoroutine(roundFlowCoroutine);
            roundFlowCoroutine = null;
        }

        base.OnNetworkDespawn();
    }

    // =========================================================
    // GAME START
    // =========================================================

    private IEnumerator StartGameFlow()
    {
        Phase.Value =
            RoundPhase.Warmup;

        PhaseTimeRemaining.Value =
            Mathf.CeilToInt(
                warmupDuration
            );

        TrapperClientId.Value =
            NoTrapper;

        deadPlayers.Clear();

        Debug.Log(
            "RoundManager: Waiting for all players..."
        );

        while (!AllClientsHavePlayerObjects())
        {
            yield return null;
        }

        // Everyone starts as Runner.
        SetAllPlayersAsRunners();

        Debug.Log(
            $"RoundManager: " +
            $"{warmupDuration} second warmup started."
        );

        yield return StartCoroutine(
            WarmupCountdown()
        );

        // Warmup has finished.
        BeginActiveRound();
    }

    // =========================================================
    // WARMUP
    // =========================================================

    private IEnumerator WarmupCountdown()
    {
        float elapsed = 0f;

        bool trapperSelected = false;

        int lastDisplayedSecond = -1;

        while (elapsed < warmupDuration)
        {
            int remaining =
                Mathf.CeilToInt(
                    warmupDuration - elapsed
                );

            if (remaining != lastDisplayedSecond)
            {
                PhaseTimeRemaining.Value =
                    remaining;

                lastDisplayedSecond =
                    remaining;
            }

            // Select the Trapper 5 seconds
            // before warmup ends.
            if (!trapperSelected &&
                elapsed >=
                warmupDuration -
                trapperSelectionTime)
            {
                SelectInitialTrapper();

                trapperSelected = true;
            }

            elapsed +=
                Time.deltaTime;

            yield return null;
        }

        PhaseTimeRemaining.Value = 0;
    }

    // =========================================================
    // INITIAL TRAPPER
    // =========================================================

    private void SelectInitialTrapper()
    {
        List<ulong> clients =
            new List<ulong>(
                NetworkManager.Singleton
                    .ConnectedClientsIds
            );

        if (clients.Count < 2)
        {
            TrapperClientId.Value =
                NoTrapper;

            Debug.Log(
                "RoundManager: Only one player. " +
                "No Trapper selected."
            );

            return;
        }

        ulong selectedClient =
            clients[
                Random.Range(
                    0,
                    clients.Count
                )
            ];

        TrapperClientId.Value =
            selectedClient;

        Debug.Log(
            $"RoundManager: Client {selectedClient} " +
            $"has been selected as the Trapper."
        );

        SwitchPlayerPrefab(
            selectedClient,
            PlayerRole.Trapper
        );
    }

    // =========================================================
    // BEGIN ACTIVE ROUND
    // =========================================================

    private void BeginActiveRound()
    {
        if (!IsServer)
            return;

        deadPlayers.Clear();

        SpawnPlayersForCurrentRound();

        Phase.Value =
            RoundPhase.Active;

        // Start every round at exactly 5 minutes.
        PhaseTimeRemaining.Value =
            Mathf.CeilToInt(
                roundDuration
            );

        Debug.Log(
            $"RoundManager: Round " +
            $"{CurrentRound.Value} is now ACTIVE. " +
            $"Time limit = {roundDuration} seconds."
        );

        // Start the 5-minute round timer.
        roundFlowCoroutine =
            StartCoroutine(
                ActiveRoundCountdown()
            );
    }

    // =========================================================
    // ACTIVE ROUND COUNTDOWN
    // =========================================================

    private IEnumerator ActiveRoundCountdown()
    {
        float elapsed = 0f;

        int lastDisplayedSecond = -1;

        while (elapsed < roundDuration)
        {
            // If something else already ended the round,
            // stop this countdown.
            if (Phase.Value != RoundPhase.Active)
            {
                yield break;
            }

            int remaining =
                Mathf.CeilToInt(
                    roundDuration - elapsed
                );

            if (remaining != lastDisplayedSecond)
            {
                PhaseTimeRemaining.Value =
                    Mathf.Max(
                        0,
                        remaining
                    );

                lastDisplayedSecond =
                    remaining;
            }

            elapsed +=
                Time.deltaTime;

            yield return null;
        }

        // Make sure the UI reaches exactly 00:00.
        PhaseTimeRemaining.Value = 0;

        // Time has expired.
        Debug.Log(
            "RoundManager: " +
            "Round time limit reached. " +
            "Trapper wins."
        );

        StartRoundEnding(
            false,
            true
        );
    }

    // =========================================================
    // SPAWN PLAYERS
    // =========================================================

    private void SpawnPlayersForCurrentRound()
    {
        foreach (ulong clientId
                 in NetworkManager.Singleton
                     .ConnectedClientsIds)
        {
            NetworkClient client =
                NetworkManager.Singleton
                    .ConnectedClients[clientId];

            if (client.PlayerObject == null)
                continue;

            PlayerRoleManager roleManager =
                client.PlayerObject
                    .GetComponent<PlayerRoleManager>();

            RespawnManager respawnManager =
                client.PlayerObject
                    .GetComponent<RespawnManager>();

            if (respawnManager == null)
                continue;

            bool isTrapper =
                roleManager != null &&
                roleManager.IsTrapper;

            Transform spawnPoint;

            if (isTrapper)
            {
                spawnPoint =
                    SpawnPointsManager.Instance
                        .GetTrapperSpawnPoint();
            }
            else
            {
                spawnPoint =
                    SpawnPointsManager.Instance
                        .GetSpawnPoint(clientId);
            }

            if (spawnPoint == null)
            {
                Debug.LogError(
                    $"RoundManager: No spawn point " +
                    $"for Client {clientId}."
                );

                continue;
            }

            respawnManager.ResetForNewRound(
                spawnPoint.position,
                spawnPoint.rotation
            );
        }
    }

    // =========================================================
    // PLAYER DEATH
    // =========================================================

    public void RegisterPlayerDeath(
        ulong clientId)
    {
        if (!IsServer)
            return;

        if (Phase.Value != RoundPhase.Active)
            return;

        if (deadPlayers.Contains(clientId))
            return;

        deadPlayers.Add(clientId);

        NetworkClient client =
            NetworkManager.Singleton
                .ConnectedClients.ContainsKey(clientId)
                ? NetworkManager.Singleton
                    .ConnectedClients[clientId]
                : null;

        PlayerRoleManager roleManager =
            client != null &&
            client.PlayerObject != null
                ? client.PlayerObject
                    .GetComponent<PlayerRoleManager>()
                : null;

        bool playerWasTrapper =
            roleManager != null &&
            roleManager.IsTrapper;

        Debug.Log(
            $"RoundManager: Client {clientId} died. " +
            $"Role = " +
            $"{(playerWasTrapper ? "Trapper" : "Runner")}."
        );

        // -----------------------------------------------------
        // TRAPPER DIED
        // -----------------------------------------------------

        if (playerWasTrapper)
        {
            StartRoundEnding(
                true,
                false
            );

            return;
        }

        // -----------------------------------------------------
        // ALL RUNNERS DIED
        // -----------------------------------------------------

        if (AreAllRunnersDead())
        {
            StartRoundEnding(
                false,
                false
            );
        }
    }

    // =========================================================
    // ALL RUNNERS DEAD
    // =========================================================

    private bool AreAllRunnersDead()
    {
        int runnerCount = 0;

        foreach (ulong clientId
                 in NetworkManager.Singleton
                     .ConnectedClientsIds)
        {
            NetworkClient client =
                NetworkManager.Singleton
                    .ConnectedClients[clientId];

            if (client.PlayerObject == null)
                continue;

            PlayerRoleManager roleManager =
                client.PlayerObject
                    .GetComponent<PlayerRoleManager>();

            if (roleManager == null ||
                !roleManager.IsRunner)
            {
                continue;
            }

            runnerCount++;

            RespawnManager respawnManager =
                client.PlayerObject
                    .GetComponent<RespawnManager>();

            if (respawnManager != null &&
                !respawnManager.IsDead.Value)
            {
                return false;
            }
        }

        return runnerCount > 0;
    }

    // =========================================================
    // START ROUND ENDING
    // =========================================================

    private void StartRoundEnding(
        bool trapperDied,
        bool timeExpired)
    {
        if (!IsServer)
            return;

        if (Phase.Value == RoundPhase.Ending)
            return;

        roundEndingBecauseTrapperDied =
            trapperDied;

        roundEndingBecauseTimeExpired =
            timeExpired;

        Phase.Value =
            RoundPhase.Ending;

        PhaseTimeRemaining.Value =
            Mathf.CeilToInt(
                nextRoundDelay
            );

        string reason;

        if (timeExpired)
        {
            reason =
                "Time limit reached - Trapper wins";
        }
        else if (trapperDied)
        {
            reason =
                "Trapper died";
        }
        else
        {
            reason =
                "All runners died";
        }

        Debug.Log(
            "RoundManager: Round ending. " +
            $"Reason = {reason}."
        );

        if (roundFlowCoroutine != null)
        {
            StopCoroutine(
                roundFlowCoroutine
            );
        }

        roundFlowCoroutine =
            StartCoroutine(
                RoundEndCountdown()
            );
    }

    // =========================================================
    // ROUND END COUNTDOWN
    // =========================================================

    private IEnumerator RoundEndCountdown()
    {
        float elapsed = 0f;

        int lastDisplayedSecond = -1;

        while (elapsed < nextRoundDelay)
        {
            int remaining =
                Mathf.CeilToInt(
                    nextRoundDelay - elapsed
                );

            if (remaining != lastDisplayedSecond)
            {
                PhaseTimeRemaining.Value =
                    remaining;

                lastDisplayedSecond =
                    remaining;
            }

            elapsed +=
                Time.deltaTime;

            yield return null;
        }

        PhaseTimeRemaining.Value = 0;

        StartNextRound();

        roundFlowCoroutine = null;
    }

    // =========================================================
    // NEXT ROUND
    // =========================================================

    private void StartNextRound()
    {
        if (!IsServer)
            return;

        CurrentRound.Value++;

        deadPlayers.Clear();

        Debug.Log(
            $"RoundManager: Starting Round " +
            $"{CurrentRound.Value}."
        );

        // -----------------------------------------------------
        // TIME LIMIT REACHED
        // -----------------------------------------------------
        //
        // Trapper survived the entire 5 minutes.
        // The old Trapper becomes a Runner.
        // A random current Runner becomes the new Trapper.
        // -----------------------------------------------------

        if (roundEndingBecauseTimeExpired)
        {
            ChooseReplacementTrapper();

            Debug.Log(
                "RoundManager: Time limit reached. " +
                "A random Runner will become the new Trapper."
            );
        }
        // -----------------------------------------------------
        // TRAPPER DIED
        // -----------------------------------------------------
        else if (roundEndingBecauseTrapperDied)
        {
            ChooseReplacementTrapper();
        }

        roundEndingBecauseTrapperDied =
            false;

        roundEndingBecauseTimeExpired =
            false;

        SpawnPlayersForCurrentRound();

        Phase.Value =
            RoundPhase.Active;

        PhaseTimeRemaining.Value =
            Mathf.CeilToInt(
                roundDuration
            );

        Debug.Log(
            $"RoundManager: Round " +
            $"{CurrentRound.Value} is ACTIVE. " +
            $"Time limit = {roundDuration} seconds."
        );

        // Start the next round timer.
        roundFlowCoroutine =
            StartCoroutine(
                ActiveRoundCountdown()
            );
    }

    // =========================================================
    // REPLACE TRAPPER
    // =========================================================

    private void ChooseReplacementTrapper()
    {
        ulong previousTrapper =
            TrapperClientId.Value;

        // -----------------------------------------------------
        // TURN OLD TRAPPER INTO RUNNER
        // -----------------------------------------------------

        if (previousTrapper != NoTrapper &&
            NetworkManager.Singleton
                .ConnectedClients
                .ContainsKey(previousTrapper))
        {
            SwitchPlayerPrefab(
                previousTrapper,
                PlayerRole.Runner
            );
        }

        // -----------------------------------------------------
        // FIND CURRENT RUNNERS
        // -----------------------------------------------------

        List<ulong> candidates =
            new List<ulong>();

        foreach (ulong clientId
                 in NetworkManager.Singleton
                     .ConnectedClientsIds)
        {
            if (clientId == previousTrapper)
                continue;

            candidates.Add(clientId);
        }

        // -----------------------------------------------------
        // NO REPLACEMENT
        // -----------------------------------------------------

        if (candidates.Count == 0)
        {
            TrapperClientId.Value =
                NoTrapper;

            Debug.Log(
                "RoundManager: " +
                "No replacement Trapper available."
            );

            return;
        }

        // -----------------------------------------------------
        // RANDOM NEW TRAPPER
        // -----------------------------------------------------

        ulong newTrapper =
            candidates[
                Random.Range(
                    0,
                    candidates.Count
                )
            ];

        TrapperClientId.Value =
            newTrapper;

        SwitchPlayerPrefab(
            newTrapper,
            PlayerRole.Trapper
        );

        Debug.Log(
            $"RoundManager: New Trapper = " +
            $"Client {newTrapper}."
        );
    }

    // =========================================================
    // SWITCH PLAYER PREFAB
    // =========================================================

    private bool SwitchPlayerPrefab(
        ulong clientId,
        PlayerRole desiredRole)
    {
        if (!IsServer)
            return false;

        if (runnerPrefab == null ||
            trapperPrefab == null)
        {
            Debug.LogError(
                "RoundManager: Runner or Trapper prefab " +
                "has not been assigned."
            );

            return false;
        }

        if (!NetworkManager.Singleton
                .ConnectedClients
                .ContainsKey(clientId))
        {
            return false;
        }

        NetworkClient client =
            NetworkManager.Singleton
                .ConnectedClients[clientId];

        NetworkObject oldPlayer =
            client.PlayerObject;

        if (oldPlayer == null)
            return false;

        PlayerRoleManager currentRole =
            oldPlayer.GetComponent<PlayerRoleManager>();

        // If already the correct role,
        // don't replace the network object.
        if (currentRole != null &&
            currentRole.Role.Value == desiredRole)
        {
            return true;
        }

        GameObject prefab =
            desiredRole == PlayerRole.Trapper
                ? trapperPrefab
                : runnerPrefab;

        Debug.Log(
            $"RoundManager: Switching Client {clientId} " +
            $"to {desiredRole}."
        );

        // ---------------------------------------------------------
        // DESTROY OLD PLAYER
        // ---------------------------------------------------------

        oldPlayer.Despawn(true);

        // ---------------------------------------------------------
        // CREATE NEW PLAYER
        // ---------------------------------------------------------

        GameObject newPlayer =
            Instantiate(
                prefab,
                Vector3.zero,
                Quaternion.identity
            );

        NetworkObject newNetworkObject =
            newPlayer.GetComponent<NetworkObject>();

        if (newNetworkObject == null)
        {
            Debug.LogError(
                $"RoundManager: {prefab.name} does not " +
                $"have a NetworkObject."
            );

            Destroy(newPlayer);

            return false;
        }

        // ---------------------------------------------------------
        // SPAWN AS PLAYER OBJECT
        // ---------------------------------------------------------

        newNetworkObject.SpawnAsPlayerObject(
            clientId,
            true
        );

        // ---------------------------------------------------------
        // EXPLICITLY ASSIGN ROLE
        // ---------------------------------------------------------

        PlayerRoleManager newRoleManager =
            newPlayer.GetComponent<PlayerRoleManager>();

        if (newRoleManager != null)
        {
            newRoleManager.SetRoleServer(
                desiredRole
            );
        }
        else
        {
            Debug.LogError(
                $"RoundManager: {prefab.name} is missing " +
                $"PlayerRoleManager."
            );
        }

        // ---------------------------------------------------------
        // MOVE TO CORRECT ROUND SPAWN
        // ---------------------------------------------------------

        Transform spawnPoint;

        if (desiredRole == PlayerRole.Trapper)
        {
            spawnPoint =
                SpawnPointsManager.Instance
                    .GetTrapperSpawnPoint();
        }
        else
        {
            spawnPoint =
                SpawnPointsManager.Instance
                    .GetSpawnPoint(clientId);
        }

        if (spawnPoint != null)
        {
            RespawnManager respawnManager =
                newPlayer.GetComponent<RespawnManager>();

            if (respawnManager != null)
            {
                respawnManager.ResetForNewRound(
                    spawnPoint.position,
                    spawnPoint.rotation
                );
            }
            else
            {
                newPlayer.transform.SetPositionAndRotation(
                    spawnPoint.position,
                    spawnPoint.rotation
                );
            }
        }
        else
        {
            Debug.LogError(
                $"RoundManager: No spawn point found " +
                $"for Client {clientId}."
            );
        }

        return true;
    }
    // =========================================================
    // SET EVERYONE TO RUNNER
    // =========================================================

    private void SetAllPlayersAsRunners()
    {
        foreach (ulong clientId
                 in NetworkManager.Singleton
                     .ConnectedClientsIds)
        {
            SwitchPlayerPrefab(
                clientId,
                PlayerRole.Runner
            );
        }

        TrapperClientId.Value =
            NoTrapper;
    }

    // =========================================================
    // CHECK PLAYER OBJECTS
    // =========================================================

    private bool AllClientsHavePlayerObjects()
    {
        if (NetworkManager.Singleton == null)
            return false;

        if (NetworkManager.Singleton
                .ConnectedClientsIds.Count == 0)
        {
            return false;
        }

        foreach (ulong clientId
                 in NetworkManager.Singleton
                     .ConnectedClientsIds)
        {
            if (!NetworkManager.Singleton
                    .ConnectedClients
                    .ContainsKey(clientId))
            {
                return false;
            }

            if (NetworkManager.Singleton
                    .ConnectedClients[clientId]
                    .PlayerObject == null)
            {
                return false;
            }
        }

        return true;
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

        // -----------------------------------------------------
        // TRAPPER DISCONNECTED
        // -----------------------------------------------------

        if (Phase.Value == RoundPhase.Active &&
            clientId == TrapperClientId.Value)
        {
            StartRoundEnding(
                true,
                false
            );

            return;
        }

        // -----------------------------------------------------
        // RUNNER DISCONNECTED
        // -----------------------------------------------------

        if (Phase.Value == RoundPhase.Active)
        {
            if (AreAllRunnersDead())
            {
                StartRoundEnding(
                    false,
                    false
                );
            }
        }
    }
}