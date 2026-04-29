using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EarWaxGunkScript : MonoBehaviour
{
    public enum WaxState { Frozen, FreeFloating, StuckToTool }

    [Header("Settings")]
    public LayerMask wallLayer;
    public LayerMask toolLayer;
    public string waxTag = "Wax";
    
    [Header("Tool Interaction")]
    public string stickySurfaceTag = "StickyTarget"; 
    
    // The gap between surfaces must be this small to stick.
    public float surfaceTouchThreshold = 0.02f; 

    [Header("Physics Tuning")]
    public float freezeThreshold = 0.1f; 
    
    private Rigidbody rb;
    private Collider myCollider;
    public WaxState currentState = WaxState.Frozen;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        myCollider = GetComponent<Collider>();
        rb.maxDepenetrationVelocity = 1.0f;
        Freeze();
    }

    private void FixedUpdate()
    {
        if (currentState == WaxState.FreeFloating)
        {
            if (rb.linearVelocity.magnitude < freezeThreshold)
            {
                if (IsTouchingStableSupport()) Freeze();
            }
        }
    }

    // ---------------------------------------------------------
    //  STRICT TRIGGER MECHANIC
    // ---------------------------------------------------------
    private void OnTriggerStay(Collider other)
    {
        if (currentState == WaxState.StuckToTool) return;

        if (IsInLayer(other.gameObject, toolLayer) && other.CompareTag(stickySurfaceTag))
        {
            // 1. Find the point on the Tool closest to my center
            // Note: If we are inside the trigger, this returns our own position!
            Vector3 pointOnTool = other.ClosestPoint(transform.position);

            // 2. Find the point on ME closest to that tool point
            Vector3 pointOnMe = myCollider.ClosestPoint(pointOnTool);

            // 3. Calculate gap. If we are overlapping, this is 0.
            float actualGap = Vector3.Distance(pointOnTool, pointOnMe);

            if (actualGap <= surfaceTouchThreshold)
            {
                if (CheckLineOfSight(pointOnTool, other))
                {
                    AttachToTool(other);
                }
            }
        }
    }

    // ---------------------------------------------------------
    //  COLLISION LOGIC
    // ---------------------------------------------------------
    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == WaxState.StuckToTool) return;

        GameObject hitObj = collision.gameObject;

        if (IsInLayer(hitObj, toolLayer))
        {
            if (!hitObj.CompareTag(stickySurfaceTag)) Unfreeze();
        }
        else if (IsInLayer(hitObj, wallLayer))
        {
            Freeze();
        }
        else if (hitObj.CompareTag(waxTag))
        {
            EarWaxGunkScript neighbor = hitObj.GetComponent<EarWaxGunkScript>();
            if (neighbor != null && neighbor.currentState == WaxState.Frozen) Freeze();
        }
    }

    // --- SUPPORT CHECK ---
    private bool IsTouchingStableSupport()
    {
        float checkRadius = transform.localScale.x * 0.6f;
        Collider[] hits = Physics.OverlapSphere(transform.position, checkRadius);
        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (IsInLayer(hit.gameObject, wallLayer)) return true;
            if (hit.CompareTag(waxTag))
            {
                EarWaxGunkScript s = hit.GetComponent<EarWaxGunkScript>();
                if (s != null && s.currentState == WaxState.Frozen) return true;
            }
        }
        return false;
    }

    // --- ACTIONS ---
    private void Freeze()
    {
        currentState = WaxState.Frozen;
        rb.useGravity = false;
        rb.isKinematic = false; 
        rb.constraints = RigidbodyConstraints.FreezeAll; 
        rb.linearVelocity = Vector3.zero;
        if (transform.parent != null && IsInLayer(transform.parent.gameObject, toolLayer)) 
            transform.SetParent(null);
    }

    private void Unfreeze()
    {
        if (currentState == WaxState.StuckToTool) return;
        currentState = WaxState.FreeFloating;
        rb.constraints = RigidbodyConstraints.None;
        rb.useGravity = true; 
        rb.isKinematic = false;
    }

    private void AttachToTool(Collider toolCollider)
    {
        currentState = WaxState.StuckToTool;
        
        // 1. Kill Physics immediately
        rb.isKinematic = true; 
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 2. Parent it exactly where it is.
        // We removed the "SnapToSurface" logic because it was causing the teleportation.
        // Since we are checking for distance < 0.02f, it is visually close enough already.
        transform.SetParent(toolCollider.transform);
    }

    // --- UTILITIES ---
    private bool CheckLineOfSight(Vector3 targetPoint, Collider targetCollider)
    {
        Vector3 startPos = transform.position;
        Vector3 direction = targetPoint - startPos;
        
        // If the distance is near zero (we are inside), we define it as visible.
        if (direction.magnitude < 0.001f) return true;

        RaycastHit hit;
        if (Physics.Raycast(startPos, direction.normalized, out hit, direction.magnitude, toolLayer))
        {
            return hit.collider == targetCollider;
        }
        return true; 
    }

    private bool IsInLayer(GameObject obj, LayerMask mask) { return ((1 << obj.layer) & mask) != 0; }
}