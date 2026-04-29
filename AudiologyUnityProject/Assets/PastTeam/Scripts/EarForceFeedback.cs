using UnityEngine;

public class EarForceFeedback : MonoBehaviour
{
    [Range(0, 800)] public float stiffness = 300f;
    [Range(0, 3)]   public float damping   = 1f;

    private MeshCollider _meshCollider;
    private Collider     _cursorCollider;

    private void Awake()
    {
        _meshCollider = GetComponent<MeshCollider>();

        // Cursor Model (child of Cursor) carries the SphereCollider
        // Tag is "Player" — use that to find it reliably
        GameObject cursorObj = GameObject.FindWithTag("Player");
        if (cursorObj != null)
            _cursorCollider = cursorObj.GetComponent<Collider>();

        if (_cursorCollider == null)
            Debug.LogWarning("[EarForceFeedback] Could not find cursor collider. " +
                             "Make sure the Cursor Model is tagged 'Player'.");

        FindFirstObjectByType<HapticManager>().RegisterEar(this);
    }

    /// <summary>
    /// Called from HapticManager on the haptic thread (device mode) or Update (mouse mode).
    /// Uses Physics.ComputePenetration so it works with non-convex MeshColliders.
    /// </summary>
    public Vector3 CalculateForce(Vector3 cursorPosition, Vector3 cursorVelocity, float cursorRadius)
    {
        if (_meshCollider == null || _cursorCollider == null)
            return Vector3.zero;

        // Use the collider's actual world transform — works in both mouse and device mode
        bool penetrating = Physics.ComputePenetration(
            _cursorCollider, _cursorCollider.transform.position, _cursorCollider.transform.rotation,
            _meshCollider,   transform.position,                 transform.rotation,
            out Vector3 direction, out float distance
        );

        if (penetrating)
        {
            // direction points from ear surface toward cursor (pushout direction)
            return direction * distance * stiffness - cursorVelocity * damping;
        }

        return Vector3.zero;
    }
}
