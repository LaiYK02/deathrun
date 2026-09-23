using System.Collections;
using TMPro;
using UnityEngine;

public class DeathBannerItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform bannerRect;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Width")]
    [SerializeField] private float minimumWidth = 250f;
    [SerializeField] private float maximumWidth = 500f;
    [SerializeField] private float horizontalPadding = 35f;
    [SerializeField] private float skullWidth = 40f;
    [SerializeField] private float spacing = 15f;

    [Header("Display")]
    [SerializeField] private float displayDuration = 5f;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    private Coroutine lifeCoroutine;

    // =========================================================
    // SETUP
    // =========================================================

    public void Setup(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Player";
        }

        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        UpdateBannerWidth();

        StartLifeCycle();
    }

    // =========================================================
    // UPDATE WIDTH
    // =========================================================

    private void UpdateBannerWidth()
    {
        if (playerNameText == null)
            return;

        if (bannerRect == null)
            return;

        // Force TextMeshPro to calculate the text size.
        playerNameText.ForceMeshUpdate();

        float textWidth =
            playerNameText.preferredWidth;

        float calculatedWidth =
            skullWidth +
            spacing +
            textWidth +
            horizontalPadding;

        float finalWidth =
            Mathf.Clamp(
                calculatedWidth,
                minimumWidth,
                maximumWidth
            );

        Vector2 size =
            bannerRect.sizeDelta;

        size.x = finalWidth;

        bannerRect.sizeDelta = size;
    }

    // =========================================================
    // START LIFE CYCLE
    // =========================================================

    private void StartLifeCycle()
    {
        if (lifeCoroutine != null)
        {
            StopCoroutine(lifeCoroutine);
        }

        lifeCoroutine =
            StartCoroutine(
                LifeCycle()
            );
    }

    // =========================================================
    // LIFE CYCLE
    // =========================================================

    private IEnumerator LifeCycle()
    {
        SetAlpha(0f);

        // -----------------------------------------------------
        // FADE IN
        // -----------------------------------------------------

        yield return Fade(
            0f,
            1f,
            fadeInDuration
        );

        // -----------------------------------------------------
        // STAY VISIBLE
        // -----------------------------------------------------

        yield return new WaitForSeconds(
            displayDuration
        );

        // -----------------------------------------------------
        // FADE OUT
        // -----------------------------------------------------

        yield return Fade(
            1f,
            0f,
            fadeOutDuration
        );

        // -----------------------------------------------------
        // DESTROY
        // -----------------------------------------------------

        Destroy(gameObject);
    }

    // =========================================================
    // FADE
    // =========================================================

    private IEnumerator Fade(
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        if (canvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            float alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    // =========================================================
    // SET ALPHA
    // =========================================================

    private void SetAlpha(
        float alpha)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha =
            Mathf.Clamp01(alpha);
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (lifeCoroutine != null)
        {
            StopCoroutine(
                lifeCoroutine
            );

            lifeCoroutine = null;
        }
    }
}