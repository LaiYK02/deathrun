using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DeathSpectatorManager : MonoBehaviour
{
    private RespawnManager localPlayer;

    private bool spectatorActive;

    private RespawnManager currentTarget;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        InvokeRepeating(
            nameof(TryFindLocalPlayer),
            0.1f,
            0.2f
        );
    }

    // =========================================================
    // FIND LOCAL PLAYER
    // =========================================================

    private void TryFindLocalPlayer()
    {
        if (localPlayer != null)
        {
            CancelInvoke(
                nameof(TryFindLocalPlayer)
            );

            return;
        }

        if (Unity.Netcode.NetworkManager.Singleton == null)
            return;

        if (!Unity.Netcode.NetworkManager.Singleton.IsClient)
            return;

        Unity.Netcode.NetworkObject playerObject =
            Unity.Netcode.NetworkManager.Singleton
                .LocalClient?
                .PlayerObject;

        if (playerObject == null)
            return;

        localPlayer =
            playerObject.GetComponent<
                RespawnManager
            >();

        if (localPlayer != null)
        {
            CancelInvoke(
                nameof(TryFindLocalPlayer)
            );
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (localPlayer == null)
            return;

        // -----------------------------------------------------
        // PLAYER ALIVE
        // -----------------------------------------------------

        if (!localPlayer.IsDead.Value)
        {
            spectatorActive = false;
            currentTarget = null;

            return;
        }

        // -----------------------------------------------------
        // ENTER SPECTATOR
        // -----------------------------------------------------

        if (!spectatorActive)
        {
            EnterSpectator();
        }

        // -----------------------------------------------------
        // MOUSE 1
        // -----------------------------------------------------

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            SpectateNextLivingPlayer();
        }
    }

    // =========================================================
    // ENTER SPECTATOR
    // =========================================================

    private void EnterSpectator()
    {
        spectatorActive = true;

        currentTarget =
            localPlayer;

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.EnterSpectatorMode(
                localPlayer.transform
            );
        }

        Debug.Log(
            "DeathSpectatorManager: " +
            "Spectator mode started."
        );
    }

    // =========================================================
    // NEXT LIVING PLAYER
    // =========================================================

    private void SpectateNextLivingPlayer()
    {
        List<RespawnManager> livingPlayers =
            GetLivingPlayers();

        // -----------------------------------------------------
        // NO LIVING PLAYERS
        // -----------------------------------------------------

        if (livingPlayers.Count == 0)
        {
            // Stay on the dead player's body.
            currentTarget =
                localPlayer;

            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.SetSpectatorTarget(
                    localPlayer.transform
                );
            }

            Debug.Log(
                "DeathSpectatorManager: " +
                "No living players. Staying on dead body."
            );

            return;
        }

        // -----------------------------------------------------
        // FIND NEXT PLAYER
        // -----------------------------------------------------

        int currentIndex =
            livingPlayers.IndexOf(
                currentTarget
            );

        int nextIndex;

        if (currentIndex < 0)
        {
            nextIndex = 0;
        }
        else
        {
            nextIndex =
                (currentIndex + 1) %
                livingPlayers.Count;
        }

        currentTarget =
            livingPlayers[nextIndex];

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.SetSpectatorTarget(
                currentTarget.transform
            );
        }

        Debug.Log(
            $"DeathSpectatorManager: " +
            $"Now spectating Client " +
            $"{currentTarget.OwnerClientId}."
        );
    }

    // =========================================================
    // GET LIVING PLAYERS
    // =========================================================

    private List<RespawnManager>
        GetLivingPlayers()
    {
        RespawnManager[] players =
            FindObjectsByType<RespawnManager>(
                FindObjectsSortMode.None
            );

        List<RespawnManager> livingPlayers =
            new List<RespawnManager>();

        foreach (RespawnManager player in players)
        {
            if (player == null)
                continue;

            if (!player.IsSpawned)
                continue;

            if (player == localPlayer)
                continue;

            if (player.IsDead.Value)
                continue;

            livingPlayers.Add(player);
        }

        // Make spectator order deterministic.
        livingPlayers.Sort(
            (a, b) =>
                a.OwnerClientId.CompareTo(
                    b.OwnerClientId
                )
        );

        return livingPlayers;
    }
}