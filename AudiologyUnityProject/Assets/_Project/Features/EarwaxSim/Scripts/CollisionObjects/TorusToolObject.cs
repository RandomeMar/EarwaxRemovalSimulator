using EarwaxSim;
using UnityEngine;

/// <summary>
/// Test torus shaped tool collision object.
/// </summary>
public class TorusToolObject : DynamicCollisionObject
{
    [Header("Torus Dimensions")]
    public float rMajor;
    public float rMinor;

    protected override CollisionShape BuildShapeTree()
    {
        return new TorusShape(Vector3.zero, Quaternion.identity, rMajor, rMinor);
    }

    private void OnValidate()
    {
        this.Awake();
    }
}
