using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NameTagManager : MonoBehaviour
{
    public static NameTagManager Instance { get; private set; }

    [Header("Name Tag UI")]
    [SerializeField] private RectTransform nameTagCanvas;
    [SerializeField] private TMP_Text nameTagPrefab;

    private readonly List<PlayerNameTag> players =
        new List<PlayerNameTag>();

    private Camera targetCamera;

    public RectTransform CanvasRect =>
        nameTagCanvas;

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

    private void Start()
    {
        RefreshCamera();
        RegisterExistingPlayers();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        RefreshCamera();

        if (scene.name == "GameScene")
        {
            RegisterExistingPlayers();
        }
    }

    private void RefreshCamera()
    {
        targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogWarning(
                "NameTagManager: Main Camera was not found."
            );
        }
    }

    private void RegisterExistingPlayers()
    {
        PlayerNameTag[] existingPlayers =
            FindObjectsByType<PlayerNameTag>(
                FindObjectsSortMode.None
            );

        foreach (PlayerNameTag player in existingPlayers)
        {
            RegisterPlayer(player);
        }
    }

    public void RegisterPlayer(
        PlayerNameTag player)
    {
        if (player == null)
            return;

        if (players.Contains(player))
            return;

        if (nameTagCanvas == null)
        {
            Debug.LogError(
                "NameTagManager: Name Tag Canvas is not assigned."
            );
            return;
        }

        if (nameTagPrefab == null)
        {
            Debug.LogError(
                "NameTagManager: Name Tag Prefab is not assigned."
            );
            return;
        }

        players.Add(player);

        TMP_Text newTag =
            Instantiate(
                nameTagPrefab,
                nameTagCanvas
            );

        RectTransform tagRect =
            newTag.GetComponent<RectTransform>();

        player.SetUI(
            tagRect,
            newTag
        );
    }

    public void UnregisterPlayer(
        PlayerNameTag player)
    {
        if (player == null)
            return;

        players.Remove(player);
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            RefreshCamera();

            if (targetCamera == null)
                return;
        }

        if (nameTagCanvas == null)
            return;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            PlayerNameTag player =
                players[i];

            if (player == null)
            {
                players.RemoveAt(i);
                continue;
            }

            player.UpdateScreenPosition(
                targetCamera,
                nameTagCanvas
            );
        }
    }
}
