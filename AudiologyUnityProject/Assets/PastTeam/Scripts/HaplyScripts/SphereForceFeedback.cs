using UnityEngine;

public class SphereForceFeedback : MonoBehaviour
{
    [Range(0, 800)]
    public float stiffness = 300f;

    [Range(0, 3)]
    public float damping = 1f;

    private Vector3 _ballPosition;
    private float _ballRadius;

    private void Awake()
    {
        _ballPosition = transform.position;
        _ballRadius = transform.lossyScale.x / 2f;

        FindObjectOfType<HapticManager>().RegisterSphere(this);
    }

    public Vector3 CalculateForce(Vector3 cursorPosition, Vector3 cursorVelocity, float cursorRadius)
    {
        Vector3 force = Vector3.zero;

        Vector3 distanceVector = cursorPosition - _ballPosition;
        float distance = distanceVector.magnitude;
        float penetration = cursorRadius + _ballRadius - distance;

        if (penetration > 0)
        {
            Vector3 normal = distanceVector.normalized;
            force = normal * penetration * stiffness;
            force -= cursorVelocity * damping;
        }

        return force;
    }
}
