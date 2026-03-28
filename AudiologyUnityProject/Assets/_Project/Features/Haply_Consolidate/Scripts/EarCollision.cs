using UnityEngine;

public class EarCollision : MonoBehaviour
{
    [Header("Force Feedback")]
    [Range(0, 800)]
    public float stiffness = 300f;

    [Range(0, 3)]
    public float damping = 1f;

    [Header("Collision")]
    public Collider cursorCollider;
    public float skinWidth = 0.002f;

    [Header("Visual Depenetration")]
    public bool applyVisualDepenetration = true;
    public int solverIterations = 4;

    [Header("Debug")]
    public bool showDebugRays = true;
    public bool logToConsole = true;

    private MeshCollider _meshCollider;
    private float _logTimer;

    // Cached last-frame results (readable from haptic thread)
    private volatile bool _isPenetrating;
    private Vector3 _lastPushDir;
    private float _lastPushDist;

    private void Awake()
    {
        _meshCollider = GetComponent<MeshCollider>();

        if (_meshCollider == null)
        {
            Debug.LogError($"[EarCollision] No MeshCollider on '{name}'. Add one!");
            return;
        }

        // Auto-find cursor collider by tag
        if (cursorCollider == null)
        {
            GameObject cursorObj = GameObject.FindWithTag("Player");
            if (cursorObj != null)
                cursorCollider = cursorObj.GetComponent<Collider>();
        }

        if (cursorCollider == null)
            Debug.LogWarning("[EarCollision] No cursor collider assigned and none found with tag 'Player'.");

        // Register with HapticManager
        var hm = FindFirstObjectByType<HapticManager>();
        if (hm != null)
            hm.RegisterEarCollision(this);
        else
            Debug.LogWarning("[EarCollision] No HapticManager found. Force feedback won't work, " +
                             "but visual depenetration will still function if called from a mouse controller.");

        Debug.Log($"[EarCollision] Initialized on '{name}' | " + $"meshCollider={(_meshCollider != null ? "OK" : "MISSING")} " + $"convex={(_meshCollider != null ? _meshCollider.convex.ToString() : "?")} | " +
                  $"cursorCollider={( cursorCollider != null ? cursorCollider.name : "NULL")} | " + $"stiffness={stiffness} damping={damping}");
    }


    public Vector3 CalculateForce(Vector3 cursorPosition, Vector3 cursorVelocity, float cursorRadius)
    {
        if (_meshCollider == null || cursorCollider == null)
            return Vector3.zero;

        // ComputePenetration uses the collider transforms as they are right now
        bool penetrating = Physics.ComputePenetration(
            cursorCollider, cursorCollider.transform.position, cursorCollider.transform.rotation,
            _meshCollider, transform.position, transform.rotation,
            out Vector3 direction, out float distance
        );

        // Cache for debug / visual depenetration
        _isPenetrating = penetrating;
        _lastPushDir = direction;
        _lastPushDist = distance;

        if (!penetrating)
            return Vector3.zero;

        // Spring-damper force ,push cursor out of the mesh
        Vector3 force = direction * distance * stiffness - cursorVelocity * damping;
        return force;
    }

    public Vector3 ResolveVisualPenetration(Transform cursorTransform)
    {
        if (_meshCollider == null || cursorCollider == null || cursorTransform == null)
            return cursorTransform != null ? cursorTransform.position : Vector3.zero;

        Vector3 resolvedPos = cursorTransform.position;
        int totalPen = 0;

        for (int iter = 0; iter < solverIterations; iter++)
        {
            cursorTransform.position = resolvedPos;
            Physics.SyncTransforms();

            bool penetrating = Physics.ComputePenetration(
                cursorCollider, cursorTransform.position, cursorTransform.rotation,
                _meshCollider, transform.position, transform.rotation,
                out Vector3 dir, out float dist
            );

            if (penetrating && dist > 0.0001f)
            {
                resolvedPos += dir * (dist + skinWidth);
                totalPen++;
            }
            else
            {
                break;
            }
        }

        // Debug
        _logTimer += Time.deltaTime;
        if (logToConsole && _logTimer >= 1f)
        {
            _logTimer = 0f;
            Debug.Log($"[EarCollision] pos={resolvedPos} | penetrations={totalPen} | " + $"lastDir={_lastPushDir} lastDist={_lastPushDist:F4}");
        }

        if (showDebugRays && totalPen > 0)
        {
            Debug.DrawRay(resolvedPos, _lastPushDir * 0.05f, Color.red, 0.1f);
        }

        return resolvedPos;
    }

    public bool IsPenetrating => _isPenetrating;
    public Vector3 LastPushDirection => _lastPushDir;
    public float LastPushDistance => _lastPushDist;
}
