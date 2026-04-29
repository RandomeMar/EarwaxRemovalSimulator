using UnityEngine;
using Haply.Inverse.DeviceControllers;
using Haply.Inverse.DeviceData;

public class ComplexMeshHaptics : MonoBehaviour
{
    [Header("Device Setup")]
    public Inverse3Controller inverse3;
    // Drag the Head (or object with the Collider) here
    public Collider targetCollider; 

    [Header("Feel Settings")]
    public float stiffness = 600.0f; // Higher because walls should feel solid
    public float damping = 10.0f;
    public float cursorRadius = 0.01f; // 1cm probe size

    [Header("Safety")]
    public bool enableForce = false;

    // --- DATA BRIDGE (Transferring data between threads) ---
    // These are the "Ghost" coordinates of the surface
    private Vector3 _cachedSurfacePoint;
    private Vector3 _cachedSurfaceNormal;
    private bool _isTouching = false;
    
    // We need these matrices to do math without Unity's helper functions
    private Matrix4x4 _worldToLocal;
    private Matrix4x4 _localToWorld;

    void OnEnable()
    {
        if (inverse3 == null) inverse3 = FindFirstObjectByType<Inverse3Controller>();
        
        if (inverse3 != null)
        {
            inverse3.DeviceStateChanged -= OnDeviceStateChanged;
            inverse3.DeviceStateChanged += OnDeviceStateChanged;
        }
    }

    void OnDisable()
    {
        if (inverse3 != null) inverse3.DeviceStateChanged -= OnDeviceStateChanged;
    }

    // --- 1. MAIN THREAD (SLOW - 90Hz) ---
    // Here we have access to the heavy Mesh data
    void Update()
    {
        if (inverse3 == null || targetCollider == null) return;

        // A. Update Matrices for the fast thread to use later
        _localToWorld = inverse3.transform.localToWorldMatrix;
        _worldToLocal = inverse3.transform.worldToLocalMatrix;

        // B. Where is the cursor right now? (In World Space)
        Vector3 cursorLocal = inverse3.CursorLocalPosition;
        Vector3 cursorWorld = inverse3.transform.TransformPoint(cursorLocal);

        // C. ASK THE COLLIDER: "Where is the closest point to me?"
        // This is the heavy math that can only happen here.
        Vector3 closestPoint = targetCollider.ClosestPoint(cursorWorld);

        // D. Calculate vector from Surface -> Cursor
        Vector3 distVec = cursorWorld - closestPoint;
        float dist = distVec.magnitude;

        // E. Store Data for the Haptic Thread
        if (dist < cursorRadius)
        {
            _isTouching = true;
            _cachedSurfacePoint = closestPoint;
            
            // Calculate the Normal (direction usually pointing OUT of the head)
            if (dist > 0.0001f)
                _cachedSurfaceNormal = distVec.normalized;
            else
                _cachedSurfaceNormal = Vector3.up; // Fallback if exactly inside
                
            // VISUAL DEBUG: Draw a little disc where we touch
            Debug.DrawLine(cursorWorld, closestPoint, Color.red);
            Debug.DrawRay(closestPoint, _cachedSurfaceNormal * 0.05f, Color.yellow);
        }
        else
        {
            _isTouching = false;
        }
    }

    // --- 2. HAPTIC THREAD (FAST - 4000Hz) ---
    // We use the cached data from the Main Thread to render the force
    void OnDeviceStateChanged(object sender, Inverse3EventArgs args)
    {
        // Safety Check
        if (!_isTouching || !enableForce)
        {
            args.DeviceController.SetCursorLocalForce(Vector3.zero);
            return;
        }

        // 1. Get Current Position
        // Note: Even though the surface data is 1 frame old (11ms lag), 
        // we use the LIVE cursor position for the spring calculation to keep it smooth.
        Vector3 localPos = args.DeviceController.CursorLocalPosition;
        Vector3 localVel = args.DeviceController.CursorLocalVelocity;
        
        // Convert to World using our Matrix (Thread-safe)
        Vector3 worldPos = _localToWorld.MultiplyPoint3x4(localPos);
        Vector3 worldVel = _localToWorld.MultiplyVector(localVel);

        // 2. Render the "Ghost Plane"
        // We pretend the complex mesh is just a flat plane at the last known contact point.
        // This feels very smooth for solid objects.
        
        // Project our position onto the surface normal to find penetration depth
        // Formula: dot(position - planePoint, planeNormal)
        Vector3 offset = worldPos - _cachedSurfacePoint;
        float distanceIdeally = Vector3.Dot(offset, _cachedSurfaceNormal);

        // If distanceIdeally is negative, we are "behind" the ghost wall
        // But we also want to account for the cursor radius (thickness)
        float penetration = cursorRadius - distanceIdeally;

        Vector3 totalForceWorld = Vector3.zero;

        if (penetration > 0)
        {
            // Spring Force pushing along the Normal
            Vector3 spring = _cachedSurfaceNormal * (penetration * stiffness);
            
            // Damping Force opposing velocity
            Vector3 damper = -worldVel * damping;

            totalForceWorld = spring + damper;
        }

        // 3. Apply
        totalForceWorld = Vector3.ClampMagnitude(totalForceWorld, 5.0f); // Safety Clamp
        Vector3 localForce = _worldToLocal.MultiplyVector(totalForceWorld);
        
        args.DeviceController.SetCursorLocalForce(localForce);
    }
}