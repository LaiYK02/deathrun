using Unity.Collections;
using Unity.Netcode;
using TMPro;
using UnityEngine;

public class PlayerNameTag : MonoBehaviour
{
    [Header("References")]
    public Transform headAnchor;

    [Header("Distance Scaling")]
    [SerializeField] private float minimumDistance = 5f;
    [SerializeField] private float maximumDistance = 40f;

    [SerializeField] private float minimumScale = 0.5f;
    [SerializeField] private float maximumScale = 1.0f;

    private LobbyPlayer lobbyPlayer;
    private RespawnManager respawnManager;

    private RectTransform tagRect;
    private TMP_Text nameText;

    private void Awake()
    {
        lobbyPlayer =
            GetComponent<LobbyPlayer>();

        respawnManager =
            GetComponent<RespawnManager>();
    }

    private void Start()
    {
        if (NameTagManager.Instance == null)
        {
            Debug.LogWarning(
                "PlayerNameTag: NameTagManager was not found in the scene."
            );

            return;
        }

        NameTagManager.Instance.RegisterPlayer(this);

        if (lobbyPlayer != null)
        {
            lobbyPlayer.PlayerName.OnValueChanged +=
                OnPlayerNameChanged;

            UpdatePlayerName(
                lobbyPlayer.PlayerName.Value.ToString()
            );
        }
    }

    private void OnDestroy()
    {
        if (lobbyPlayer != null)
        {
            lobbyPlayer.PlayerName.OnValueChanged -=
                OnPlayerNameChanged;
        }

        if (NameTagManager.Instance != null)
        {
            NameTagManager.Instance.UnregisterPlayer(this);
        }
    }

    private void OnPlayerNameChanged(
        FixedString64Bytes previousName,
        FixedString64Bytes newName)
    {
        UpdatePlayerName(
            newName.ToString()
        );
    }

    private void UpdatePlayerName(
        string playerName)
    {
        if (nameText == null)
            return;

        if (string.IsNullOrWhiteSpace(playerName))
        {
            nameText.text = "Player";
            return;
        }

        nameText.text = playerName;
    }

    public void SetUI(
        RectTransform rect,
        TMP_Text text)
    {
        tagRect = rect;
        nameText = text;

        if (lobbyPlayer != null)
        {
            UpdatePlayerName(
                lobbyPlayer.PlayerName.Value.ToString()
            );
        }

        // Start at the maximum scale.
        if (tagRect != null)
        {
            tagRect.localScale =
                Vector3.one * maximumScale;
        }
    }

    public bool ShouldBeVisible()
    {
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsClient)
        {
            return false;
        }

        // Never show the local player's own name.
        if (NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject ==
            GetComponent<NetworkObject>())
        {
            return false;
        }

        // Hide dead players.
        if (respawnManager != null &&
            respawnManager.IsDead.Value)
        {
            return false;
        }

        // Hide all player names while pause/settings is open.
        if (PauseMenuManager.Instance != null &&
            (PauseMenuManager.Instance.IsPaused ||
             PauseMenuManager.Instance.IsSettingsOpen))
        {
            return false;
        }

        return true;
    }

    public void UpdateScreenPosition(
        Camera targetCamera,
        RectTransform canvasRect)
    {
        if (tagRect == null ||
            targetCamera == null ||
            canvasRect == null)
        {
            return;
        }

        if (!ShouldBeVisible())
        {
            tagRect.gameObject.SetActive(false);
            return;
        }

        Transform target =
            headAnchor != null
                ? headAnchor
                : transform;

        Vector3 worldPosition =
            target.position;

        // Slightly above the assigned head anchor.
        worldPosition +=
            Vector3.up * 0.08f;

        Vector3 screenPosition =
            targetCamera.WorldToScreenPoint(
                worldPosition
            );

        // Behind the camera.
        if (screenPosition.z <= 0f)
        {
            tagRect.gameObject.SetActive(false);
            return;
        }

        tagRect.gameObject.SetActive(true);

        // --------------------------------------------------
        // DISTANCE-BASED SCALE
        // --------------------------------------------------

        float distance =
            Vector3.Distance(
                targetCamera.transform.position,
                worldPosition
            );

        float scale =
            Mathf.InverseLerp(
                minimumDistance,
                maximumDistance,
                distance
            );

        // Inverse because:
        // Close = maximum scale
        // Far   = minimum scale
        float currentScale =
            Mathf.Lerp(
                maximumScale,
                minimumScale,
                scale
            );

        tagRect.localScale =
            Vector3.one * currentScale;

        // --------------------------------------------------
        // SCREEN POSITION
        // --------------------------------------------------

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                null,
                out Vector2 localPosition
            );

        tagRect.anchoredPosition =
            localPosition;
    }
}