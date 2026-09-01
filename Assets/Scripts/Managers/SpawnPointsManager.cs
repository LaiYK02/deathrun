using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnPointsManager : MonoBehaviour
{
    public static SpawnPointsManager Instance { get; private set; }

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    // Keeps each connected player assigned to a specific spawn point.
    private readonly Dictionary<ulong, int> playerSpawnAssignments =
        new Dictionary<ulong, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Automatically find spawn points if none were assigned.
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            FindSpawnPoints();
        }
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback +=
                OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback +=
                OnClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -=
                OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback -=
                OnClientDisconnected;
        }
    }

    private void FindSpawnPoints()
    {
        GameObject[] objects =
            GameObject.FindGameObjectsWithTag("Respawn");

        List<Transform> points = new List<Transform>();

        foreach (GameObject obj in objects)
        {
            points.Add(obj.transform);
        }

        // Sort by name so:
        // SpawnPoint_1
        // SpawnPoint_2
        // SpawnPoint_3
        // SpawnPoint_4
        // are always in the expected order.
        points.Sort((a, b) =>
            string.Compare(
                a.name,
                b.name,
                System.StringComparison.Ordinal
            )
        );

        spawnPoints = points.ToArray();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        AssignSpawnPoint(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (playerSpawnAssignments.ContainsKey(clientId))
        {
            playerSpawnAssignments.Remove(clientId);

            Debug.Log(
                $"SpawnPointsManager: Released spawn point for Client {clientId}."
            );
        }
    }

    private void AssignSpawnPoint(ulong clientId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError(
                "SpawnPointsManager: No spawn points available!"
            );

            return;
        }

        // Already assigned?
        if (playerSpawnAssignments.ContainsKey(clientId))
            return;

        int spawnIndex = FindAvailableSpawnPoint();

        playerSpawnAssignments.Add(clientId, spawnIndex);

        Debug.Log(
            $"SpawnPointsManager: Client {clientId} assigned to " +
            $"SpawnPoint_{spawnIndex + 1}."
        );
    }

    private int FindAvailableSpawnPoint()
    {
        // Find the first spawn point that isn't currently assigned.
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            bool alreadyUsed = false;

            foreach (int assignedIndex in playerSpawnAssignments.Values)
            {
                if (assignedIndex == i)
                {
                    alreadyUsed = true;
                    break;
                }
            }

            if (!alreadyUsed)
                return i;
        }

        // If there are more players than spawn points,
        // reuse the first point.
        return 0;
    }

    public Transform GetSpawnPoint(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
            return null;

        if (!playerSpawnAssignments.ContainsKey(clientId))
        {
            AssignSpawnPoint(clientId);
        }

        int spawnIndex =
            playerSpawnAssignments[clientId];

        return spawnPoints[spawnIndex];
    }

    public Vector3 GetSpawnPosition(ulong clientId)
    {
        Transform spawnPoint = GetSpawnPoint(clientId);

        if (spawnPoint == null)
            return Vector3.zero;

        return spawnPoint.position;
    }

    public Quaternion GetSpawnRotation(ulong clientId)
    {
        Transform spawnPoint = GetSpawnPoint(clientId);

        if (spawnPoint == null)
            return Quaternion.identity;

        return spawnPoint.rotation;
    }
}