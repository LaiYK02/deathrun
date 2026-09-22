using System.Collections;
using TMPro;
using UnityEngine;

public class EndRoundBannerManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup bannerCanvasGroup;
    [SerializeField] private TMP_Text endRoundText;

    [Header("Fade Settings")]
    [SerializeField, Min(0f)]
    private float fadeInDuration = 0.35f;

    [SerializeField, Min(0f)]
    private float fadeOutDuration = 0.5f;

    [SerializeField, Min(0f)]
    private float fadeOutDelay = 0f;

    private Coroutine fadeCoroutine;

    private RoundPhase lastPhase =
        RoundPhase.Warmup;

    private bool initialized;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // If CanvasGroup was not manually assigned,
        // automatically get/add one to this object.

        if (bannerCanvasGroup == null)
        {
            bannerCanvasGroup =
                GetComponent<CanvasGroup>();

            if (bannerCanvasGroup == null)
            {
                bannerCanvasGroup =
                    gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Start hidden.
        SetBannerAlpha(0f);

        bannerCanvasGroup.interactable = false;
        bannerCanvasGroup.blocksRaycasts = false;
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (RoundManager.Instance == null)
        {
            Debug.LogWarning(
                "EndRoundBannerManager: " +
                "RoundManager was not found."
            );

            return;
        }

        lastPhase =
            RoundManager.Instance.Phase.Value;

        initialized = true;

        // Handle the current state immediately.
        // This is useful if the UI starts while the round
        // is already in Ending or Active.

        HandlePhase(
            lastPhase
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!initialized)
            return;

        if (RoundManager.Instance == null)
            return;

        RoundPhase currentPhase =
            RoundManager.Instance.Phase.Value;

        if (currentPhase == lastPhase)
            return;

        lastPhase =
            currentPhase;

        HandlePhase(
            currentPhase
        );
    }

    // =========================================================
    // HANDLE PHASE
    // =========================================================

    private void HandlePhase(
        RoundPhase phase)
    {
        switch (phase)
        {
            // -------------------------------------------------
            // WARMUP
            // -------------------------------------------------

            case RoundPhase.Warmup:

                HideImmediately();

                break;

            // -------------------------------------------------
            // ACTIVE
            // -------------------------------------------------

            case RoundPhase.Active:
                
                StartFadeOut();

                break;

            // -------------------------------------------------
            // ENDING
            // -------------------------------------------------

            case RoundPhase.Ending:

                StartShowWinner();

                break;
        }
    }

    // =========================================================
    // SHOW WINNER
    // =========================================================

    private void StartShowWinner()
    {
        if (RoundManager.Instance == null)
            return;

        if (endRoundText == null)
        {
            Debug.LogError(
                "EndRoundBannerManager: " +
                "End Round Text is not assigned."
            );

            return;
        }

        RoundEndResult result =
            RoundManager.Instance
                .CurrentRoundEndResult.Value;

        // -----------------------------------------------------
        // WAIT IF RESULT HAS NOT ARRIVED YET
        // -----------------------------------------------------

        if (result == RoundEndResult.None)
        {
            StartFadeCoroutine(
                WaitForWinnerResult()
            );

            return;
        }

        ApplyWinnerText(
            result
        );

        StartFadeIn();
    }

    // =========================================================
    // WAIT FOR WINNER RESULT
    // =========================================================

    private IEnumerator WaitForWinnerResult()
    {
        float timeout = 1f;

        while (
            timeout > 0f &&
            RoundManager.Instance != null &&
            RoundManager.Instance.Phase.Value ==
                RoundPhase.Ending &&
            RoundManager.Instance.CurrentRoundEndResult.Value ==
                RoundEndResult.None
        )
        {
            timeout -= Time.deltaTime;

            yield return null;
        }

        if (RoundManager.Instance == null)
            yield break;

        if (RoundManager.Instance.Phase.Value !=
            RoundPhase.Ending)
        {
            yield break;
        }

        RoundEndResult result =
            RoundManager.Instance
                .CurrentRoundEndResult.Value;

        if (result == RoundEndResult.None)
        {
            Debug.LogWarning(
                "EndRoundBannerManager: " +
                "Could not determine round winner."
            );

            yield break;
        }

        ApplyWinnerText(
            result
        );

        StartFadeIn();
    }

    // =========================================================
    // APPLY WINNER TEXT
    // =========================================================

    private void ApplyWinnerText(
        RoundEndResult result)
    {
        if (endRoundText == null)
            return;

        switch (result)
        {
            case RoundEndResult.TrapperWin:

                endRoundText.text =
                    "Trapper Win!";

                break;

            case RoundEndResult.RunnersWin:

                endRoundText.text =
                    "Runners Win!";

                break;

            default:

                endRoundText.text =
                    "";

                break;
        }
    }

    // =========================================================
    // FADE IN
    // =========================================================

    private void StartFadeIn()
    {
        StartFadeCoroutine(
            FadeCanvasGroup(
                bannerCanvasGroup.alpha,
                1f,
                fadeInDuration
            )
        );
    }

    // =========================================================
    // FADE OUT
    // =========================================================

    private void StartFadeOut()
    {
        StartFadeCoroutine(
            FadeOutCoroutine()
        );
    }

    // =========================================================
    // FADE OUT COROUTINE
    // =========================================================

    private IEnumerator FadeOutCoroutine()
    {
        if (fadeOutDelay > 0f)
        {
            yield return new WaitForSeconds(
                fadeOutDelay
            );
        }

        yield return FadeCanvasGroup(
            bannerCanvasGroup.alpha,
            0f,
            fadeOutDuration
        );
    }

    // =========================================================
    // FADE
    // =========================================================

    private IEnumerator FadeCanvasGroup(
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        if (bannerCanvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            SetBannerAlpha(
                targetAlpha
            );

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

            // Smooth ease-in/ease-out.
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

            SetBannerAlpha(
                alpha
            );

            yield return null;
        }

        SetBannerAlpha(
            targetAlpha
        );
    }

    // =========================================================
    // START FADE COROUTINE
    // =========================================================

    private void StartFadeCoroutine(
        IEnumerator routine)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );

            fadeCoroutine = null;
        }

        fadeCoroutine =
            StartCoroutine(
                FadeWrapper(routine)
            );
    }

    // =========================================================
    // FADE WRAPPER
    // =========================================================

    private IEnumerator FadeWrapper(
        IEnumerator routine)
    {
        yield return routine;

        fadeCoroutine = null;
    }

    // =========================================================
    // HIDE IMMEDIATELY
    // =========================================================

    private void HideImmediately()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );

            fadeCoroutine = null;
        }

        SetBannerAlpha(0f);
    }

    // =========================================================
    // SET ALPHA
    // =========================================================

    private void SetBannerAlpha(
        float alpha)
    {
        if (bannerCanvasGroup == null)
            return;

        bannerCanvasGroup.alpha =
            Mathf.Clamp01(alpha);

        bannerCanvasGroup.interactable =
            false;

        bannerCanvasGroup.blocksRaycasts =
            false;
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );

            fadeCoroutine = null;
        }
    }
}