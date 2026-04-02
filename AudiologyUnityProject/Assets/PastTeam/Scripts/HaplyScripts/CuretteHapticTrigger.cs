using UnityEngine;

public class CuretteHapticTrigger : MonoBehaviour
{
    public bool isTouchingCube { get; private set; } = false;
    public Vector3 lastCollisionNormal { get; private set; } = Vector3.up;

    [Header("Vibration Settings")]
    [Range(0, 10)]
    public float vibrationAmplitude = 2f;
    [Range(0, 100)]
    public float vibrationFrequency = 50f;

    private float localTime = 0f; // Local timer safe for threading

    private void Awake()
    {
        FindFirstObjectByType<HapticManager>().RegisterCuretteTrigger(this);
    }

    private void Update()
    {
        localTime += Time.deltaTime; // Update our safe time
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Cube"))
        {
            isTouchingCube = true;
            Vector3 contactPoint = other.ClosestPoint(transform.position);
            lastCollisionNormal = (transform.position - contactPoint).normalized;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Cube"))
        {
            isTouchingCube = false;
            lastCollisionNormal = Vector3.up; // Reset
        }
    }

    public Vector3 CalculateForce(Vector3 cursorPosition, Vector3 cursorVelocity, float cursorRadius)
    {
        if (!isTouchingCube)
            return Vector3.zero;

        float oscillation = Mathf.Sin(2.0f * Mathf.PI * vibrationFrequency * localTime);
        Vector3 vibrationForce = vibrationAmplitude * oscillation * lastCollisionNormal;

        return vibrationForce;
    }
}
