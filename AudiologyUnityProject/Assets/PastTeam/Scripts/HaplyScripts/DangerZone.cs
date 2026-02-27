using UnityEngine;

public class DangerZone : MonoBehaviour
{
    public float dangerRadius = 0.01f;
    public string warningMessage;

    private Vector3 _cubePosition;
    private Vector3 _cubeSize;
    private float _penetration = 0f;

    private void Awake()
    {
        _cubePosition = transform.position;
        _cubeSize = transform.lossyScale;
        FindFirstObjectByType<HapticManager>().RegisterDangerZone(this);
    }

    public float GetPenetrationDepth(Vector3 cursorPosition, float cursorRadius)
    {
        Vector3 closestPoint = new Vector3(
            Mathf.Clamp(cursorPosition.x, _cubePosition.x - _cubeSize.x / 2f, _cubePosition.x + _cubeSize.x / 2f),
            Mathf.Clamp(cursorPosition.y, _cubePosition.y - _cubeSize.y / 2f, _cubePosition.y + _cubeSize.y / 2f),
            Mathf.Clamp(cursorPosition.z, _cubePosition.z - _cubeSize.z / 2f, _cubePosition.z + _cubeSize.z / 2f)
        );

        Vector3 distanceVector = cursorPosition - closestPoint;
        float distance = distanceVector.magnitude;
        float penetration = cursorRadius - distance;

        _penetration = Mathf.Max(penetration, 0f);
        return _penetration;
    }

    public float NormalizedPenetration()
    {
        return Mathf.Clamp01(_penetration / dangerRadius);
    }
}
