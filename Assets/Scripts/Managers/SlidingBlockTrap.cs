using UnityEngine;

public class SlidingBlockTrap : MonoBehaviour
{
    private const float TargetZ = -5f;
    private const float MoveDuration = 1f;
    private const float HoldDuration = 3f;

    [SerializeField] private PowerButtonInteraction powerButton;

    private float originalZ;

    private void Awake()
    {
        originalZ = transform.localPosition.z;
    }

    private void Update()
    {
        float z = powerButton.TryGetActivePressElapsedTime(out double elapsedSeconds)
            ? GetPositionZ(originalZ, elapsedSeconds)
            : originalZ;

        Vector3 position = transform.localPosition;
        if (position.z == z)
            return;

        position.z = z;
        transform.localPosition = position;
    }

    private static float GetPositionZ(float startZ, double elapsedSeconds)
    {
        if (elapsedSeconds < MoveDuration)
            return Mathf.Lerp(startZ, TargetZ, (float)(elapsedSeconds / MoveDuration));

        double returnTime = elapsedSeconds - MoveDuration - HoldDuration;
        return Mathf.Lerp(TargetZ, startZ, (float)(returnTime / MoveDuration));
    }
}
