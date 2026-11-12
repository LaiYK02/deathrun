using Unity.Netcode;
using UnityEngine;

internal static class SlidingDoorCrush
{
    private const float ContactTolerance = 0.0001f;

    public static void ResolveClosingStep(BoxCollider block1, BoxCollider block2, float closingDistance)
    {
        // The player owner drives its CharacterController and reports deaths to the server.
        NetworkObject player = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (player == null || !player.TryGetComponent(out PlayerRoleManager role) || !role.IsRunner ||
            !player.TryGetComponent(out RespawnManager respawn) || respawn.IsDead.Value || respawn.DeathTriggeredLocally ||
            !player.TryGetComponent(out CharacterController controller) || !controller.enabled)
        {
            return;
        }

        Transform playerTransform = controller.transform;
        Vector3 scale = playerTransform.lossyScale;
        Vector3 center = playerTransform.TransformPoint(controller.center);
        float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float halfSpine = Mathf.Max(0f, controller.height * Mathf.Abs(scale.y) * 0.5f - radius);
        float worldClosingDistance = block1.transform.parent.TransformVector(Vector3.forward * closingDistance).z;

        if (IsCrushed(GetDoorBounds(block1), GetDoorBounds(block2), center, radius, halfSpine,
            worldClosingDistance, out float pushDistance))
        {
            respawn.RequestDeath();
        }
        else if (pushDistance != 0f)
        {
            Vector3 push = Vector3.forward * pushDistance;
            if (controller.isGrounded)
                push.y = -(controller.skinWidth + controller.minMoveDistance);

            controller.Move(push);
        }
    }

    private static Bounds GetDoorBounds(BoxCollider door)
    {
        // The doors translate along world Z. Their physics bounds may still have last tick's center.
        Bounds bounds = door.bounds;
        bounds.center = door.transform.TransformPoint(door.center);
        return bounds;
    }

    private static bool IsCrushed(Bounds block1, Bounds block2, Vector3 center, float radius,
        float halfSpine, float closingDistance, out float pushDistance)
    {
        pushDistance = 0f;
        float positiveFace = block1.min.z;
        float negativeFace = block2.max.z;
        if (center.z < negativeFace || center.z > positiveFace)
            return false;

        float positiveReach = GetCapsuleReach(block1, center, radius, halfSpine);
        float negativeReach = GetCapsuleReach(block2, center, radius, halfSpine);

        // Each inner face limits where the capsule's center can move. Crossing limits means it cannot fit.
        float maxCenter = positiveReach > 0f ? positiveFace - closingDistance - positiveReach : float.PositiveInfinity;
        float minCenter = negativeReach > 0f ? negativeFace + closingDistance + negativeReach : float.NegativeInfinity;
        if (minCenter > maxCenter + ContactTolerance)
            return true;

        pushDistance = Mathf.Clamp(center.z, minCenter, Mathf.Max(minCenter, maxCenter)) - center.z;
        return false;
    }

    private static float GetCapsuleReach(Bounds door, Vector3 center, float radius, float halfSpine)
    {
        Vector3 min = door.min;
        Vector3 max = door.max;
        float lateralGap = Mathf.Max(0f, Mathf.Max(min.x - center.x, center.x - max.x));
        float verticalGap = Mathf.Max(0f, Mathf.Max(min.y - center.y - halfSpine, center.y - halfSpine - max.y));
        float reachSquared = radius * radius - lateralGap * lateralGap - verticalGap * verticalGap;
        return Mathf.Sqrt(Mathf.Max(0f, reachSquared));
    }
}
