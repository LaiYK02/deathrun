using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FloatingNotificationManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text notificationText;

    [Header("Movement")]
    [SerializeField] private float moveDistance = 60f;
    [SerializeField] private float moveUpTime = 0.35f;
    [SerializeField] private float stayTime = 2f;
    [SerializeField] private float moveDownTime = 0.35f;

    [Header("Fade")]
    [SerializeField] private float fadeInTime = 0.25f;
    [SerializeField] private float fadeOutTime = 0.35f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Vector2 initialPosition;

    private Coroutine notificationCoroutine;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        canvasGroup =
            GetComponent<CanvasGroup>();

        initialPosition =
            rectTransform.anchoredPosition;

        // Start hidden.
        // IMPORTANT:
        // Do NOT deactivate the GameObject.
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    // =========================================================
    // SHOW NOTIFICATION
    // =========================================================

    public void ShowNotification(string message)
    {
        if (notificationText == null)
        {
            Debug.LogWarning(
                "FloatingNotificationManager: " +
                "Notification Text is not assigned."
            );

            return;
        }

        // Stop previous notification if one is running.
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }

        notificationCoroutine =
            StartCoroutine(
                NotificationAnimation(message)
            );
    }

    // =========================================================
    // NOTIFICATION ANIMATION
    // =========================================================

    private IEnumerator NotificationAnimation(
        string message)
    {
        notificationText.text = message;

        // Reset position.
        rectTransform.anchoredPosition =
            initialPosition;

        // Start completely transparent.
        canvasGroup.alpha = 0f;

        // -----------------------------------------------------
        // MOVE UP + FADE IN
        // -----------------------------------------------------

        Vector2 startPosition =
            initialPosition;

        Vector2 upPosition =
            startPosition +
            Vector2.up * moveDistance;

        yield return Animate(
            startPosition,
            upPosition,
            0f,
            1f,
            Mathf.Max(
                moveUpTime,
                fadeInTime
            )
        );

        // -----------------------------------------------------
        // STAY
        // -----------------------------------------------------

        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(
            stayTime
        );

        // -----------------------------------------------------
        // MOVE DOWN + FADE OUT
        // -----------------------------------------------------

        Vector2 downPosition =
            upPosition -
            Vector2.up * moveDistance;

        yield return Animate(
            upPosition,
            downPosition,
            1f,
            0f,
            Mathf.Max(
                moveDownTime,
                fadeOutTime
            )
        );

        // -----------------------------------------------------
        // RESET
        // -----------------------------------------------------

        canvasGroup.alpha = 0f;

        rectTransform.anchoredPosition =
            initialPosition;

        notificationCoroutine = null;
    }

    // =========================================================
    // ANIMATE
    // =========================================================

    private IEnumerator Animate(
        Vector2 startPosition,
        Vector2 targetPosition,
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        if (duration <= 0f)
        {
            rectTransform.anchoredPosition =
                targetPosition;

            canvasGroup.alpha =
                targetAlpha;

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            // Smooth movement.
            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            rectTransform.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    smoothProgress
                );

            // Fade.
            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    progress
                );

            yield return null;
        }

        rectTransform.anchoredPosition =
            targetPosition;

        canvasGroup.alpha =
            targetAlpha;
    }
}