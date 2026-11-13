using UnityEngine;

public class StaggeredPlatformTrap : MonoBehaviour
{
    private const float LoweredY = -5f;
    private const float MoveDuration = 2f;
    private const float HoldDuration = 3f;
    private const float StartInterval = 0.5f;

    [SerializeField] private PowerButtonInteraction powerButton;
    [SerializeField] private Transform[] platforms;

    private float[] originalHeights;

    private void Awake()
    {
        originalHeights = new float[platforms.Length];
        for (int i = 0; i < platforms.Length; i++)
            originalHeights[i] = platforms[i].localPosition.y;
    }

    private void Update()
    {
        bool active = powerButton.TryGetActivePressElapsedTime(out double elapsedSeconds);

        for (int i = 0; i < platforms.Length; i++)
        {
            float height = active ? GetPlatformHeight(originalHeights[i], i, elapsedSeconds) : originalHeights[i];
            Vector3 position = platforms[i].localPosition;
            if (position.y == height)
                continue;

            position.y = height;
            platforms[i].localPosition = position;
        }
    }

    private static float GetPlatformHeight(float originalY, int platformIndex, double elapsedSeconds)
    {
        double platformTime = elapsedSeconds - platformIndex * StartInterval;
        if (platformTime < MoveDuration)
            return Mathf.Lerp(originalY, LoweredY, (float)(platformTime / MoveDuration));

        double returnTime = platformTime - MoveDuration - HoldDuration;
        return Mathf.Lerp(LoweredY, originalY, (float)(returnTime / MoveDuration));
    }
}
