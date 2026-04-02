using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [Header("Force Settings")]
    [Range(0, 100)] public float pushForce = 0.01f;
    [Range(0, 100)] public float stickiness = 30f;
    [Range(0, 10)] public float breakawayForce = 3f;

    [Header("Squish Visuals")]
    [Range(0, 1)] public float squishAmount = 0.2f;
    [Range(0, 10)] public float squishSpeed = 5f;
    public Transform visualSphere;

    [Header("Spline Tube Reference")]
    public CurvedTubeForceFeedback curvedTube;

    private Rigidbody rb;
    private float sphereRadius;
    private Vector3 mainThreadPosition;
    private Vector3 initialScale;
    private Vector3 pendingForce = Vector3.zero;

    private bool stuck = true;
    private float currentSquish = 0f;
    private float targetSquish = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        sphereRadius = GetComponent<SphereCollider>().radius * transform.lossyScale.x;

        if (visualSphere == null && transform.childCount > 0)
            visualSphere = transform.GetChild(0);

        initialScale = visualSphere != null ? visualSphere.localScale : Vector3.one;

        FindObjectOfType<HapticManager>()?.RegisterMovingSphere(this);

        if (curvedTube == null)
            curvedTube = FindObjectOfType<CurvedTubeForceFeedback>();
    }

    private void Update()
    {
        mainThreadPosition = transform.position;

        if (pendingForce != Vector3.zero)
        {
            if (stuck)
            {
                rb.AddForce(-rb.linearVelocity * stickiness, ForceMode.Force);
                if (rb.linearVelocity.magnitude > breakawayForce)
                    stuck = false;
            }

            rb.AddForce(pendingForce, ForceMode.Force);
            pendingForce = Vector3.zero;
        }

        // ✅ Apply outward radial force from spline center
        if (curvedTube != null)
        {
            Vector3 radialForce = curvedTube.CalculateRadialWallForce(transform.position, rb.linearVelocity, sphereRadius);
            rb.AddForce(radialForce, ForceMode.Force);
        }

        currentSquish = Mathf.Lerp(currentSquish, targetSquish, squishSpeed * Time.deltaTime);
        ApplySquish(currentSquish);
    }

    /// <summary>
    /// HapticManager calls this to compute collision resistance and build pushing force.
    /// </summary>
    public Vector3 CalculateForce(Vector3 cursorPos, Vector3 cursorVel, float cursorRadius)
    {
        Vector3 delta = cursorPos - mainThreadPosition;
        float distance = delta.magnitude;
        float penetration = cursorRadius + sphereRadius - distance;

        if (penetration > 0f)
        {
            Vector3 normal = delta.normalized;

            Vector3 force = normal * penetration * 100f;
            force -= cursorVel * 10f;

            pendingForce += -normal * pushForce;
            targetSquish = 1f;
            return force;
        }

        targetSquish = 0f;
        return Vector3.zero;
    }

    private void ApplySquish(float factor)
    {
        if (visualSphere == null) return;

        float stretch = 1f + factor * squishAmount;
        float squash = 1f - factor * squishAmount;

        visualSphere.localScale = new Vector3(
            initialScale.x * stretch,
            initialScale.y * squash,
            initialScale.z * stretch
        );
    }
}
