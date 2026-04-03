using UnityEngine;

public class ComputePenCollisionTest : MonoBehaviour
{
    [Header("References")]
    public Transform earMesh;
    public Camera cam;

    [Header("Mouse Settings")]
    public float scrollSpeed = 0.1f;

    [Header("Rotation (right-click drag)")]
    public float rotateSensitivity = 0.3f;

    [Header("Collision")]
    public float collisionRadius = 0.05f;
    public int solverIterations = 4;
    public float skinWidth = 0.005f;

    [Header("Debug")]
    public bool showDebugRays = true;
    public bool logToConsole = true;
    
    private float    _depth;
    private Vector3  _lastMousePos;
    private float    _logTimer;

    private SphereCollider _sphereCol;
    private Collider       _earCollider;
    private Vector3        _collisionOffset;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        // sets a sphere collider if one doesnt exist.
        _sphereCol = GetComponent<SphereCollider>();
        if (_sphereCol == null)
            _sphereCol = gameObject.AddComponent<SphereCollider>();

        _sphereCol.isTrigger = true;            // trigger so it doesn't physically push things
        _sphereCol.radius = collisionRadius;
        _sphereCol.center = Vector3.zero;

        // debug logs to see whats happening *keep for now*
        if (earMesh != null)
            _earCollider = earMesh.GetComponent<Collider>();

        Debug.Log($"[ComputePen] earMesh={(earMesh ? earMesh.name : "NULL")}, " + $"earColliderType={(_earCollider != null ? _earCollider.GetType().Name : "NULL")}, " + $"sphereRadius={collisionRadius:F4}, skinWidth={skinWidth:F4}");

        if (_earCollider != null)
        {
            var b = _earCollider.bounds;
            Debug.Log($"[ComputePen] Ear bounds center={b.center} size={b.size}");
            Debug.Log($"[ComputePen] Ear collider convex={(_earCollider is MeshCollider mc ? mc.convex.ToString() : "N/A")}");
        }
    }

    private void Update()
    {
        // Keep sphere radius in sync with inspector
        if (_sphereCol != null && Mathf.Abs(_sphereCol.radius - collisionRadius) > 0.0001f)
            _sphereCol.radius = collisionRadius;

        // gets mouse position
        Vector3 desiredPos = GetMouseDesiredPosition();

        // applies collision offset from last frame (if any)
        Vector3 candidatePos = desiredPos + _collisionOffset;
        transform.position = candidatePos;

        // force physics update so ComputePenetration uses the latest position
        Physics.SyncTransforms();

        // resolves collisions and gets final position
        Vector3 resolvedPos = ResolveCollision(candidatePos);
        transform.position = resolvedPos;

        // updates persistent collision offset for next frame
        _collisionOffset = resolvedPos - desiredPos;

        // clamps the offset to prevent drifting too far from the mouse position (can happen if the ear is very thick and the solver pushes a long way out)
        float maxOffset = (collisionRadius + skinWidth) * 3f;
        if (_collisionOffset.magnitude > maxOffset)
            _collisionOffset = _collisionOffset.normalized * maxOffset;

        HandleRotation();
    }

    //desired mouse position
    private Vector3 GetMouseDesiredPosition()
    {
        if (cam == null || earMesh == null) return transform.position;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0f)
            _depth += scroll * scrollSpeed;

        Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);
        Plane earPlane = new Plane(-cam.transform.forward, earMesh.position);
        if (earPlane.Raycast(mouseRay, out float enter))
        {
            Vector3 hitPoint = mouseRay.GetPoint(enter);
            return hitPoint + cam.transform.forward * _depth;
        }

        return transform.position;
    }

    //right click rotation for mouse input (when using mouse not haply)
    private void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1))
            _lastMousePos = Input.mousePosition;

        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - _lastMousePos;
            _lastMousePos = Input.mousePosition;
            transform.Rotate(-delta.y * rotateSensitivity, delta.x * rotateSensitivity, 0f, Space.World);
        }
    }

    // Collision based on compute penetration, returns the resolved position after depenetration
    private Vector3 ResolveCollision(Vector3 startPos)
    {
        if (_sphereCol == null || _earCollider == null) return startPos;

        Vector3 center = startPos;

        _logTimer += Time.deltaTime;
        bool log = logToConsole && _logTimer >= 1f;
        int totalPenetrations = 0;
        Vector3 lastDir = Vector3.zero;
        float lastDist = 0f;

        for (int iter = 0; iter < solverIterations; iter++)
        {
            // updates the sphere's world position for ComputePenetration
            transform.position = center;
            Physics.SyncTransforms();

            bool penetrating = Physics.ComputePenetration(
                _sphereCol, transform.position, transform.rotation,
                _earCollider, _earCollider.transform.position, _earCollider.transform.rotation,
                out Vector3 direction, out float distance
            );

            if (penetrating && distance > 0.0001f)
            {
                // Add skinWidth so we push slightly beyond the surface
                float pushDist = distance + skinWidth;
                center += direction * pushDist;
                totalPenetrations++;
                lastDir = direction;
                lastDist = distance;

                if (showDebugRays)
                    Debug.DrawRay(center, direction * pushDist, Color.red, 0.1f);
            }
            else
            {
                break; // No more penetration
            }
        }

        // Debug visualization
        if (showDebugRays)
        {
            Color c = totalPenetrations > 0 ? Color.red : Color.green;
            float r = collisionRadius;
            Debug.DrawRay(center + Vector3.left * r, Vector3.right * r * 2f, c);
            Debug.DrawRay(center + Vector3.down * r, Vector3.up * r * 2f, c);
            Debug.DrawRay(center + Vector3.back * r, Vector3.forward * r * 2f, c);
        }

        if (log)
        {
            _logTimer = 0f;
            Debug.Log($"[ComputePen] pos={center} | penetrations={totalPenetrations} | " + $"lastDir={lastDir} lastDist={lastDist:F4} | " + $"radius={collisionRadius:F4} | offset={_collisionOffset}");

            if (_earCollider != null)
            {
                bool inside = _earCollider.bounds.Contains(center);
                Debug.Log($"[ComputePen] insideEarBounds={inside} " + $"boundsCenter={_earCollider.bounds.center} boundsSize={_earCollider.bounds.size}");
            }
        }

        return center;
    }
}
