using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathBannerManager : MonoBehaviour
{
    public static DeathBannerManager Instance { get; private set; }

    [Header("Banner")]
    [SerializeField] private DeathBannerItem deathBannerPrefab;

    [SerializeField] private RectTransform bannerContainer;

    [Header("Stacking")]
    [SerializeField] private float verticalSpacing = 8f;

    [SerializeField] private int maximumVisibleBanners = 5;

    private readonly List<DeathBannerItem> activeBanners =
        new List<DeathBannerItem>();

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
    // SHOW DEATH BANNER
    // =========================================================

    public void ShowDeathBanner(
        string playerName)
    {
        if (deathBannerPrefab == null)
        {
            Debug.LogError(
                "DeathBannerManager: " +
                "Death Banner Prefab is not assigned."
            );

            return;
        }

        if (bannerContainer == null)
        {
            Debug.LogError(
                "DeathBannerManager: " +
                "Banner Container is not assigned."
            );

            return;
        }

        // -----------------------------------------------------
        // REMOVE NULL ENTRIES
        // -----------------------------------------------------

        CleanupNullBanners();

        // -----------------------------------------------------
        // MAXIMUM BANNERS
        // -----------------------------------------------------

        if (activeBanners.Count >=
            maximumVisibleBanners)
        {
            DeathBannerItem oldest =
                activeBanners[0];

            if (oldest != null)
            {
                Destroy(oldest.gameObject);
            }

            activeBanners.RemoveAt(0);
        }

        // -----------------------------------------------------
        // CREATE NEW BANNER
        // -----------------------------------------------------

        DeathBannerItem newBanner =
            Instantiate(
                deathBannerPrefab,
                bannerContainer
            );

        newBanner.transform.SetAsLastSibling();

        newBanner.Setup(
            playerName
        );

        activeBanners.Add(
            newBanner
        );

        RefreshBannerPositions();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void LateUpdate()
    {
        CleanupNullBanners();

        RefreshBannerPositions();
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void CleanupNullBanners()
    {
        for (
            int i = activeBanners.Count - 1;
            i >= 0;
            i--
        )
        {
            if (activeBanners[i] == null)
            {
                activeBanners.RemoveAt(i);
            }
        }
    }

    // =========================================================
    // POSITION BANNERS
    // =========================================================

    private void RefreshBannerPositions()
    {
        if (activeBanners.Count == 0)
            return;

        float currentY = 0f;

        for (
            int i = activeBanners.Count - 1;
            i >= 0;
            i--
        )
        {
            DeathBannerItem banner =
                activeBanners[i];

            if (banner == null)
                continue;

            RectTransform rect =
                banner.GetComponent<RectTransform>();

            if (rect == null)
                continue;

            rect.anchoredPosition =
                new Vector2(
                    0f,
                    -currentY
                );

            currentY +=
                rect.rect.height +
                verticalSpacing;
        }
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}