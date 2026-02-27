using UnityEngine;
using Haply.Inverse.DeviceControllers;
using Haply.Inverse.DeviceData;

public class CylinderForceFeedback : MonoBehaviour
{
    [Range(0, 800)] public float stiffness = 100f;
    [Range(0, 3)] public float damping = 0.5f;
    [Range(0.01f, 1f)] public float wallThickness = 0.2f;

    private Inverse3Controller inverse3;
    private Vector3 center;
    private float radius;
    private float halfLength;
    private float cursorRadius;

    private void Awake()
    {
        inverse3 = FindFirstObjectByType<Inverse3Controller>();
        inverse3.Ready.AddListener((device, args) => CacheData());
        FindFirstObjectByType<HapticManager>()?.RegisterCylinder(this);
    }

    private void CacheData()
    {
        center = transform.position;
        radius = transform.lossyScale.x / 2f;     // X = diameter
        halfLength = transform.lossyScale.z / 2f; // Z = height along tube
        cursorRadius = inverse3.Cursor.Radius;
    }

    public Vector3 CalculateForce(Vector3 cursorPos, Vector3 cursorVel, float cradius)
    {
        Vector3 force = Vector3.zero;

        Vector3 local = cursorPos - center;

        // Check Z bounds (tube length)
        if (Mathf.Abs(local.z) > halfLength)
            return force;

        // Calculate distance from center axis in XY plane
        Vector3 radial = new Vector3(local.x, local.y, 0);
        float distance = radial.magnitude;

        float innerWall = radius - wallThickness - cradius;
        float outerWall = radius + cradius;

        if (distance >= innerWall && distance <= outerWall)
        {
            float penetration = outerWall - distance;
            Vector3 normal = radial.normalized;
            force = normal * penetration * stiffness;
            force -= cursorVel * damping;

            Debug.Log($"[Haptic Contact] RadialDist: {distance:F3}, Penetration: {penetration:F3}");
        }

        return force;
    }
}
