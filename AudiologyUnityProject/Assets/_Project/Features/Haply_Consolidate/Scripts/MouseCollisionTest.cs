using UnityEngine;

public class MouseCollisionTest : MonoBehaviour
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
    public float rayMaxDist = 0.5f;
    public int rayDirCount = 26;
    public int solverIterations = 4;

    [Header("Debug")]
    public bool showDebugRays = true;
    public bool logToConsole = true;

    private float    _depth;
    private Vector3  _lastMousePos;
    private Vector3[] _rayDirs;
    private float    _logTimer;
    private Collider _earCollider;
    private Collider[] _ownColliders;
    private LayerMask _rayMask;

    // Collision offset that persists between frames
    private Vector3 _collisionOffset;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        _rayDirs = BuildRayDirections(rayDirCount);

        if (earMesh != null)
            _earCollider = earMesh.GetComponent<Collider>();

        _ownColliders = GetComponentsInChildren<Collider>(true);
        _rayMask = ~0;

        Debug.Log($"[CollisionTest] earMesh={(earMesh ? earMesh.name : "NULL")}, " + $"ownColliders={_ownColliders.Length}, rayDirs={_rayDirs.Length}, " + $"collisionRadius={collisionRadius:F4}, rayMaxDist={rayMaxDist:F3}");

        if (_earCollider != null)
        {
            var b = _earCollider.bounds;
            Debug.Log($"[CollisionTest] Ear bounds center={b.center} size={b.size}");
        }
    }

    private void Update()
    {
        // Get where the mouse wants the curette to be
        Vector3 desiredPos = GetMouseDesiredPosition();

        // Applies the persisted collision offset (so it doesnt snap back inside)
        Vector3 candidatePos = desiredPos + _collisionOffset;
        transform.position = candidatePos;

        // resolve collision  *( push position further out )
        Vector3 resolvedPos = ResolveCollision(candidatePos);
        transform.position = resolvedPos;

        // update the collision offset (difference between resolved and desired)
        //Decay the offset slightly each frame so the curette can slide along surfaces
        //and return to mouse control when moving away from the surface
        Vector3 newOffset = resolvedPos - desiredPos;
        _collisionOffset = newOffset;

        // If mouse has moved far from collision area, decay the offset
        // so the curette returns to direct mouse control
        float offsetMag = _collisionOffset.magnitude;
        if (offsetMag > collisionRadius * 3f)
        {
            _collisionOffset = _collisionOffset.normalized * collisionRadius * 3f;
        }

        HandleRotation();
    }

    //Mouse desired position
    private Vector3 GetMouseDesiredPosition()
    {
        if (cam == null || earMesh == null) return transform.position;

        // Scroll wheel adjusts depth
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

    // Right click rotatation handler
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

    // Collision based on raycast probes, returns the resolved position after depenetration
    private Vector3 ResolveCollision(Vector3 startPos)
    {
        if (_rayDirs == null || _rayDirs.Length == 0) return startPos;

        // Disable own colliders so rays don't hit the curette itself
        bool[] wasEnabled = new bool[_ownColliders.Length];
        for (int i = 0; i < _ownColliders.Length; i++)
        {
            wasEnabled[i] = _ownColliders[i].enabled;
            _ownColliders[i].enabled = false;
        }

        Vector3 center = startPos;
        float radius = collisionRadius;

        // Debug stats
        _logTimer += Time.deltaTime;
        bool log = logToConsole && _logTimer >= 1f;
        int totalHits = 0;
        float closestDist = float.MaxValue;
        string closestName = "";
        int totalPenDetected = 0;

        for (int iter = 0; iter < solverIterations; iter++)
        {
            Vector3 totalPush = Vector3.zero;
            int penCount = 0;

            for (int i = 0; i < _rayDirs.Length; i++)
            {
                Vector3 dir = _rayDirs[i];

                //from outside inward, cast from outside the sphere towards the center (catches being just outside mesh)
                Vector3 origin = center + dir * rayMaxDist;
                if (Physics.Raycast(origin, -dir, out RaycastHit hit, rayMaxDist, _rayMask, QueryTriggerInteraction.Ignore))
                {
                    float distToHit = Vector3.Distance(center, hit.point);
                    if (iter == 0)
                    {
                        totalHits++;
                        if (distToHit < closestDist)
                        {
                            closestDist = distToHit;
                            closestName = hit.collider.name;
                        }
                    }

                    if (distToHit < radius)
                    {
                        // Penetration detected 
                        float pen = radius - distToHit;
                        totalPush += hit.normal * pen;
                        penCount++;
                        if (iter == 0) totalPenDetected++;
                    }
                }

                // Outward cast from center, catches being just inside mesh and raycast escaping out
                if (Physics.Raycast(center, dir, out RaycastHit hitOut, radius, _rayMask, QueryTriggerInteraction.Ignore))
                {
                    if (iter == 0)
                    {
                        totalHits++;
                        float d = hitOut.distance;
                        if (d < closestDist)
                        {
                            closestDist = d;
                            closestName = hitOut.collider.name;
                        }
                    }

                    float pen = radius - hitOut.distance;
                    if (pen > 0f)
                    {
                        totalPush += hitOut.normal * pen;
                        penCount++;
                        if (iter == 0) totalPenDetected++;
                    }
                }
            }

            if (penCount > 0)
            {
                Vector3 push = totalPush / penCount;
                center += push;

                if (showDebugRays)
                    Debug.DrawRay(center, push.normalized * 0.05f, Color.red, 0.1f);
            }
            else
            {
                break; // No more penetration
            }
        }

        // Debug visualization
        if (showDebugRays)
        {
            Color sphereColor = totalPenDetected > 0 ? Color.red : Color.green;
            // Draw cross at center
            Debug.DrawRay(center + Vector3.left * radius, Vector3.right * radius * 2f, sphereColor);
            Debug.DrawRay(center + Vector3.down * radius, Vector3.up * radius * 2f, sphereColor);
            Debug.DrawRay(center + Vector3.back * radius, Vector3.forward * radius * 2f, sphereColor);
        }

        if (log)
        {
            _logTimer = 0f;
            Debug.Log($"[CollisionTest] pos={center} | hits={totalHits}/52 | " +
                      $"closest='{closestName}' dist={closestDist:F4} | radius={radius:F4} | " +
                      $"penetrations={totalPenDetected} | offset={_collisionOffset}");

            if (_earCollider != null)
            {
                bool inside = _earCollider.bounds.Contains(center);
                Debug.Log($"[CollisionTest] insideEarBounds={inside} " +
                          $"boundsCenter={_earCollider.bounds.center} boundsSize={_earCollider.bounds.size}");
            }
        }

        // Reenable own colliders
        for (int i = 0; i < _ownColliders.Length; i++)
        {
            if (wasEnabled[i]) _ownColliders[i].enabled = true;
        }

        return center;
    }

    // just a helper function to build ray directions based on count (6 or 26)
    private static Vector3[] BuildRayDirections(int count)
    {
        if (count <= 6)
        {
            return new Vector3[]
            {
                Vector3.right, Vector3.left,
                Vector3.up, Vector3.down,
                Vector3.forward, Vector3.back
            };
        }

        var dirs = new System.Collections.Generic.List<Vector3>(26);
        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        for (int z = -1; z <= 1; z++)
        {
            if (x == 0 && y == 0 && z == 0) continue;
            dirs.Add(new Vector3(x, y, z).normalized);
        }
        return dirs.ToArray();
    }
}
