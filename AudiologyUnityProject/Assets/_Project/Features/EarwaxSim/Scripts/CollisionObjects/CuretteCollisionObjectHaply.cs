using EarwaxSim;
using Haply.Inverse.DeviceCursors;
using UnityEngine;

/// <summary>
/// Collision object representing the curette. (DEPRECATED)
/// </summary>
public class CuretteCollisionObjectHaply : CuretteCollisionObject
{
    Transform haplyCursorTransform;

    protected override void Awake()
    {
        base.Awake();

        if (Application.isPlaying)
        {
            var cursor = FindFirstObjectByType<Inverse3Cursor>();
            if (cursor != null)
            {
                haplyCursorTransform = cursor.transform;
            }
            else
            {
                Debug.LogWarning("[CuretteCollisionObjectHaply] No Inverse3Cursor found in scene. A Haply Cursor GameObject with an Inverse3Cursor component must be present.");
            }
        }
    }

    protected override void Update()
    {
        if (haplyCursorTransform != null)
        {
            this.targetPosition = haplyCursorTransform.position;
        }
    }
}
