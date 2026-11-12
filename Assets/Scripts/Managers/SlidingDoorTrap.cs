using UnityEngine;

public class SlidingDoorTrap : MonoBehaviour
{
    private const float SlideDistance = 6f;
    private const float SlideDuration = 1f;
    private const float HoldDuration = 3f;

    [SerializeField] private PowerButtonInteraction powerButton;
    [SerializeField] private BoxCollider block1;
    [SerializeField] private BoxCollider block2;

    private Vector3 block1ClosedPosition;
    private Vector3 block2ClosedPosition;
    private float appliedOpenFraction;

    private void Awake()
    {
        block1ClosedPosition = block1.transform.localPosition;
        block2ClosedPosition = block2.transform.localPosition;
    }

    private void LateUpdate()
    {
        bool trapActive = powerButton.TryGetActivePressElapsedTime(out double elapsedSeconds);
        float openFraction = trapActive ? GetOpenFraction(elapsedSeconds) : 0f;

        if (openFraction == appliedOpenFraction)
            return;

        if (trapActive && openFraction < appliedOpenFraction)
        {
            float closingDistance = SlideDistance * (appliedOpenFraction - openFraction);
            SlidingDoorCrush.ResolveClosingStep(block1, block2, closingDistance);
        }

        Vector3 offset = Vector3.forward * (SlideDistance * openFraction);
        block1.transform.localPosition = block1ClosedPosition + offset;
        block2.transform.localPosition = block2ClosedPosition - offset;
        appliedOpenFraction = openFraction;
    }

    private static float GetOpenFraction(double elapsedSeconds)
    {
        if (elapsedSeconds < SlideDuration)
            return Mathf.Clamp01((float)(elapsedSeconds / SlideDuration));

        double closingElapsed = elapsedSeconds - SlideDuration - HoldDuration;
        return 1f - Mathf.Clamp01((float)(closingElapsed / SlideDuration));
    }
}
