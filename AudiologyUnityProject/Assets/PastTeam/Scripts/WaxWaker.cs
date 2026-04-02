using UnityEngine;

public class WaxWaker : MonoBehaviour
{
    // Adjust this if your wax is on a specific layer, or leave empty to affect everything
    public LayerMask waxLayer;

    // Use OnTriggerEnter if your tool or wax is a Trigger
    // Use OnCollisionEnter if both are solid
    private void OnTriggerEnter(Collider other)
    {
        WakeUpWax(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        WakeUpWax(collision.collider);
    }

    private void WakeUpWax(Collider waxCollider)
    {
        // Check if the object we hit has a Rigidbody
        Rigidbody waxRb = waxCollider.attachedRigidbody;

        // If it has a Rigidbody and is currently Frozen (Kinematic)
        if (waxRb != null && waxRb.isKinematic)
        {
            // 1. Unfreeze it so it falls/moves
            waxRb.isKinematic = false;

            // 2. Add 'Drag' (CORRECTED LINE)
            // Earwax is sticky, so high drag feels better.
            waxRb.linearDamping = 5f; 
            waxRb.angularDamping = 5f;
            
            // Optional: You can also wake up the specific cube's physics explicitly
            waxRb.WakeUp();
        }
    }
}