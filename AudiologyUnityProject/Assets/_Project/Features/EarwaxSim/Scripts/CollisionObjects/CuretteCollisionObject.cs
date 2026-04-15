using EarwaxSim;
using System.Collections.Generic;
using UnityEngine;

public class CuretteCollisionObject : DynamicCollisionObject
{
    [Header("Torus Settings")]
    public float rMajor;
    public float rMinor;
    public float xRotation;

    public bool drawShape;

    ViewingLattice viewer;

    protected override CollisionShape BuildShapeTree()
    {
        return new TorusShape(Vector3.zero, new Vector3(xRotation, 0f, 0f), rMajor, rMinor);
    }

    protected override void Awake()
    {
        base.Awake();

        viewer = new(viewSize, viewResolution, viewParticleSize);
        if (useHapticInput && Application.isPlaying)
        {
            var manager = FindFirstObjectByType<HapticManager>();
            if (manager != null)
                manager.RegisterDynamicTool(this);
            else
                Debug.LogWarning(
                    $"[CuretteCollisionObject] useHapticInput is on but no HapticManager was found in the scene.");
        }
    }

    private void OnValidate()
    {
        this.Awake();
    }

    private void OnDrawGizmos()
    {
        if (viewer != null && drawShape) viewer.DrawLattice(this, viewCutoff);
    }
}
