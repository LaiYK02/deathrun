using UnityEngine;

public class RotatingCylinderTrap : MonoBehaviour
{
    [SerializeField] private PowerButtonInteraction powerButton;
    [SerializeField] private bool spinAroundY;
    [SerializeField, Min(0f)] private float rotationDuration = 5f;
    [SerializeField, Min(0f)] private float holdDuration = 1f;
    [SerializeField] private float degreesPerSecond = 150f;
    [SerializeField, Min(0f)] private float pushSpeed = 500f;

    private Quaternion originalRotation;
    private float appliedAngle;

    private void Awake()
    {
        originalRotation = transform.localRotation;
    }

    private void Update()
    {
        float angle = powerButton.TryGetActivePressElapsedTime(out double elapsedSeconds)
            ? GetAngleOffset(elapsedSeconds, degreesPerSecond, rotationDuration, holdDuration)
            : 0f;

        if (angle == appliedAngle)
            return;

        Vector3 axis = spinAroundY ? Vector3.up : Vector3.right;
        transform.localRotation = Quaternion.AngleAxis(angle, axis) * originalRotation;
        appliedAngle = angle;
    }

    public Vector3 GetRiderVelocity(Vector3 riderPosition)
    {
        if (pushSpeed <= 0f || degreesPerSecond == 0f ||
            !powerButton.TryGetActivePressElapsedTime(out double elapsedSeconds))
            return Vector3.zero;

        int spinDirection = GetSpinDirection(elapsedSeconds, rotationDuration, holdDuration);
        if (spinDirection == 0)
            return Vector3.zero;

        if (degreesPerSecond < 0f)
            spinDirection = -spinDirection;

        if (spinAroundY)
        {
            Vector3 scale = transform.lossyScale;
            // The Unity cylinder mesh is one unit across before scaling.
            float radius = 0.5f * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            return GetDiscPush(riderPosition - transform.position, radius, pushSpeed, spinDirection);
        }

        Vector3 axis = transform.parent != null ? transform.parent.right : Vector3.right;
        Vector3 direction = Vector3.Cross(axis, Vector3.up).normalized;
        return direction * (pushSpeed * spinDirection);
    }

    private static Vector3 GetDiscPush(Vector3 riderOffset, float radius, float speed, int spinDirection)
    {
        if (radius <= 0f || spinDirection == 0)
            return Vector3.zero;

        riderOffset.y = 0f;
        Vector3 radial = Vector3.ClampMagnitude(riderOffset / radius, 1f);
        Vector3 tangent = Vector3.Cross(Vector3.up, radial) * spinDirection;
        return (radial + tangent) * (speed / Mathf.Sqrt(2f));
    }

    private static int GetSpinDirection(double elapsedSeconds, float duration, float holdDuration)
    {
        if (elapsedSeconds >= 0d && elapsedSeconds < duration)
            return 1;

        double returnStart = duration + holdDuration;
        return elapsedSeconds >= returnStart && elapsedSeconds < returnStart + duration ? -1 : 0;
    }

    private static float GetAngleOffset(double elapsedSeconds, float speed, float duration, float holdDuration)
    {
        if (elapsedSeconds < duration)
            return Mathf.Clamp((float)elapsedSeconds, 0f, duration) * speed;

        double returnTime = elapsedSeconds - duration - holdDuration;
        return (duration - Mathf.Clamp((float)returnTime, 0f, duration)) * speed;
    }
}
