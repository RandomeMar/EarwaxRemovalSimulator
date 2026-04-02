using UnityEngine;

public class PlaneForceFeedback : MonoBehaviour
{
    [Range(0, 800)]
    public float stiffness = 300f;

    [Range(0, 3)]
    public float damping = 1f;

    private float planeY;

    private void Awake()
    {
        planeY = transform.position.y;
        FindObjectOfType<HapticManager>().RegisterPlane(this);
    }

    public Vector3 CalculateForce(Vector3 cursorPosition, Vector3 cursorVelocity, float cursorRadius)
    {
        Vector3 force = Vector3.zero;

        float penetration = (planeY - (cursorPosition.y - cursorRadius));

        if (penetration > 0)
        {
            Vector3 normal = Vector3.up;
            force = normal * penetration * stiffness;
            force -= cursorVelocity * damping;
        }

        return force;
    }
}
