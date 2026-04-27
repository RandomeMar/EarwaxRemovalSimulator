using Haply.Inverse.DeviceControllers;
using UnityEngine;

public class MouseCursorController : MonoBehaviour
{
    [Header("References")]
    public Transform cursorTransform;
    public Camera cam;
    public Inverse3Controller inverse3;
    public Transform target;

    [Header("Mouse Sensitivity")]
    public float sensitivity = 0.001f;

    [Header("Depth (scroll wheel)")]
    public float scrollStep = 0.05f;
    public float minOffset  = -1.0f;
    public float maxOffset  =  1.0f;

    [Header("Curette Rotation (right-click drag)")]
    public float rotateSensitivity = 0.3f;

    [Header("Collision (mouse mode only)")]
    public int   solverIterations    = 3;
    public float cursorCollisionRadius = 0.005f;
    public float raycastMaxDistance = 0.1f;
    public int   rayDirectionCount = 26;

    [Header("Debug")]
    public bool debugMode = true;

    private float      _depthOffset;
    private Vector3    _lastMousePos;
    private Vector3    _mouseDeltaAccum;   // accumulated mouse offset from target
    private bool       _initialized;
    private Collider   _cursorCollider;
    private Collider[] _allCursorColliders; // all colliders in cursor hierarchy
    private float      _debugTimer;
    private Vector3[]  _rayDirs;           // pre-computed ray directions

    private void Awake()
    {
        if (cursorTransform == null) cursorTransform = transform;
        if (cam == null)             cam = Camera.main;
        _depthOffset = 0f;

        // simple-curette MeshCollider, StickyArea BoxCollider, Cube BoxCollider)
        _allCursorColliders = cursorTransform.GetComponentsInChildren<Collider>(true);
        _cursorCollider = cursorTransform.GetComponentInChildren<Collider>();
        Debug.Log($"[MouseCursor] Found {_allCursorColliders.Length} colliders in cursor hierarchy");

        // Pre-compute ray probe directions
        _rayDirs = BuildRayDirections(rayDirectionCount);
        Debug.Log($"[MouseCursor] Using {_rayDirs.Length} ray directions, " +
                  $"collisionRadius={cursorCollisionRadius:F4}, maxDist={raycastMaxDistance:F3}");
    }

    private void Update()
    {
        // If the Haply device is connected and ready, let it drive the cursor
        if (inverse3 != null && inverse3.IsReady)
            return;

        HandleDepthScroll();
        HandleMouseMovement();
        HandleCuretteRotation();
        ResolvePenetration();
    }
    // handles depth offset adjustment via scroll wheel, clamped to a reasonable range to avoid losing the target
    private void HandleDepthScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0f)
        {
            _depthOffset += scroll * scrollStep;
            _depthOffset  = Mathf.Clamp(_depthOffset, minOffset, maxOffset);
        }
    }

    // delta based movement: cursor position is target position + accumulated mouse offset, where mouse offset is derived from pixel delta each frame and camera orientation
    private void HandleMouseMovement()
    {
        if (cam == null || target == null) return;

        // On first frame, snap cursor to target and record mouse position
        if (!_initialized)
        {
            _initialized = true;
            _mouseDeltaAccum = Vector3.zero;
            _lastMousePos = Input.mousePosition;
            cursorTransform.position = target.position;
            Debug.Log($"[MouseCursor] Initialized cursor at target: {target.position}");
            return;
        }

        // Calculate mouse delta this frame
        Vector3 mouseDelta = Input.mousePosition - _lastMousePos;
        _lastMousePos = Input.mousePosition;

        // Convert mouse pixel delta into world-space movement using camera axes
        Vector3 worldMove = cam.transform.right * (mouseDelta.x * sensitivity)
                          + cam.transform.up    * (mouseDelta.y * sensitivity);

        _mouseDeltaAccum += worldMove;

        // Final position = target origin + accumulated mouse offset + depth along camera forward
        cursorTransform.position = target.position
                                 + _mouseDeltaAccum
                                 + cam.transform.forward * _depthOffset;
    }

    //right click and drag to rotate the curette (cursor) around the target, using camera axes as reference
    private void HandleCuretteRotation()
    {
        if (Input.GetMouseButtonDown(1))
            _lastMousePos = Input.mousePosition;

        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - _lastMousePos;
            _lastMousePos = Input.mousePosition;

            cursorTransform.Rotate(
                -delta.y * rotateSensitivity,
                 delta.x * rotateSensitivity,
                0f,
                Space.World
            );
        }
    }

    // raycast based collision
    // Physics.Raycast works with all collider types including non-convex so good to have for now
    // MeshColliders. We cast rays inward from outside the cursor sphere
    // and also outward from the cursor center. If any ray hits a surface
    // closer than cursorCollisionRadius, we push the cursor out.
    private void ResolvePenetration()
    {
        if (_rayDirs == null || _rayDirs.Length == 0) return;

        // Temporarily disable all cursor colliders so rays don't self-hit
        bool[] _collidersWereEnabled = new bool[_allCursorColliders.Length];
        for (int i = 0; i < _allCursorColliders.Length; i++)
        {
            _collidersWereEnabled[i] = _allCursorColliders[i].enabled;
            _allCursorColliders[i].enabled = false;
        }

        Vector3 cursorCenter = cursorTransform.position;
        float radius = cursorCollisionRadius;

        // Debug logging
        _debugTimer += Time.deltaTime;
        bool logThisFrame = debugMode && _debugTimer >= 1f;
        if (logThisFrame)
        {
            _debugTimer = 0f;
            Debug.Log($"[MouseCursor] pos: {cursorCenter} | radius: {radius:F4}");
            if (target != null)
            {
                var targetCol = target.GetComponent<Collider>();
                if (targetCol != null)
                {
                    var bounds = targetCol.bounds;
                    float distToBounds = bounds.SqrDistance(cursorCenter);
                    bool insideBounds = bounds.Contains(cursorCenter);
                    Debug.Log($"[MouseCursor] target '{target.name}' bounds center={bounds.center} " +
                              $"size={bounds.size} sqrDist={distToBounds:F4} inside={insideBounds}");
                }
                else
                {
                    Debug.Log($"[MouseCursor] target '{target.name}' at {target.position} (NO COLLIDER)");
                }
            }

            Vector3 toTarget = (target.position - cursorCenter);
            float distToTarget = toTarget.magnitude;
            if (Physics.Raycast(cursorCenter, toTarget.normalized, out RaycastHit diagHit, distToTarget + 1f, ~0, QueryTriggerInteraction.Ignore))
                Debug.Log($"[MouseCursor] DIAG ray toward target hit '{diagHit.collider.name}' at dist={diagHit.distance:F4}");
            else
                Debug.Log($"[MouseCursor] DIAG ray toward target hit NOTHING (dist to origin={distToTarget:F4})");
        }

        // Multiple solver iterations for stability
        for (int iter = 0; iter < solverIterations; iter++)
        {
            Vector3 totalPush = Vector3.zero;
            int hitCount = 0;
            int anyRayHits = 0;       // total rays that hit anything
            float closestDist = float.MaxValue;
            string closestName = "";

            for (int i = 0; i < _rayDirs.Length; i++)
            {
                Vector3 dir = _rayDirs[i];

                // Cast inward from outside the sphere.
                Vector3 rayOrigin = cursorCenter + dir * raycastMaxDistance;
                if (Physics.Raycast(rayOrigin, -dir, out RaycastHit hitInward, raycastMaxDistance, ~0, QueryTriggerInteraction.Ignore))
                {
                    float distToSurface = Vector3.Distance(cursorCenter, hitInward.point);
                    anyRayHits++;

                    if (distToSurface < closestDist)
                    {
                        closestDist = distToSurface;
                        closestName = hitInward.collider.name;
                    }

                    if (distToSurface < radius)
                    {
                        float penetration = radius - distToSurface;
                        totalPush += hitInward.normal * penetration;
                        hitCount++;

                        if (logThisFrame && iter == 0)
                            Debug.Log($"[MouseCursor] HIT(in) '{hitInward.collider.name}' " +
                                      $"dist={distToSurface:F4} pen={penetration:F4} normal={hitInward.normal}");
                    }
                }

                // Cast outward from cursor center.
                if (Physics.Raycast(cursorCenter, dir, out RaycastHit hitOutward, radius, ~0, QueryTriggerInteraction.Ignore))
                {
                    float penetration = radius - hitOutward.distance;
                    anyRayHits++;
                    if (penetration > 0f)
                    {
                        totalPush += hitOutward.normal * penetration;
                        hitCount++;

                        if (logThisFrame && iter == 0)
                            Debug.Log($"[MouseCursor] HIT(out) '{hitOutward.collider.name}' " +
                                      $"dist={hitOutward.distance:F4} pen={penetration:F4}");
                    }
                }
            }

            if (logThisFrame && iter == 0)
                Debug.Log($"[MouseCursor] DIAG rays: {anyRayHits} hit anything, " +
                          $"closest='{closestName}' at {closestDist:F4}, penetrating={hitCount}");

            // Apply averaged push
            if (hitCount > 0)
            {
                Vector3 push = totalPush / hitCount;
                cursorTransform.position += push;
                cursorCenter += push;

                // Also adjust the accumulated mouse offset so the cursor doesn't
                // snap back into the wall on the next frame
                _mouseDeltaAccum += push;
            }
            else
            {
                break; // No penetration found, skip remaining iterations
            }
        }

        // Re-enable all cursor colliders that were previously enabled
        for (int i = 0; i < _allCursorColliders.Length; i++)
        {
            if (_collidersWereEnabled[i]) _allCursorColliders[i].enabled = true;
        }
    }

    private static Vector3[] BuildRayDirections(int count)
    {
        if (count <= 6)
        {
            return new Vector3[]
            {
                Vector3.right, Vector3.left,
                Vector3.up,    Vector3.down,
                Vector3.forward, Vector3.back
            };
        }

        // 26 directions, all combinations of {-1, 0, 1} except (0,0,0)
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

    private float GetCursorWorldRadius()
    {
        if (_cursorCollider is SphereCollider sphere)
            return sphere.radius * _cursorCollider.transform.lossyScale.x;
        return _cursorCollider != null ? _cursorCollider.bounds.extents.magnitude : cursorCollisionRadius;
    }
}
