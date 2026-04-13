using EarwaxSim;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEditor.Compilation;
using UnityEngine;

public class EarCollisionObject : CollisionObjectBase
{
    public List<CanalNode> canalNodes = new List<CanalNode>(4);

    [Header("Viewer Settings")]
    public bool drawLattice = true;
    public Vector3 viewSize;
    public Vector3Int viewResolution;
    [Min(0f)]
    public float viewParticleSize = .1f;
    [Min(0f)]
    public float viewCutoff = .1f;

    ViewingLattice viewer;

    // This method is evil incarnate. It builds the canal shape defined using CanalNodes
    protected override CollisionShape BuildShapeTree()
    {
        CollisionShape prev = null;

        if (canalNodes == null)
        {
            Debug.LogError("canalPoints is null");
        }

        for (int i = 1; i < canalNodes.Count; i++)
        {
            Vector3 a = canalNodes[i - 1].transform.localPosition;
            Vector3 b = canalNodes[i].transform.localPosition;
            Vector3 distVec = b - a;
            Vector3 center = (a + b) / 2f;

            Vector3 yAxis = distVec.normalized; // Cylinders fall along the y-axis
            Vector3 hint = canalNodes[i - 1].transform.localRotation * Vector3.forward; // CanalNode's local y rotation controls the roll of the cylinder
            Vector3 zAxis = hint - Vector3.Project(hint, yAxis);

            if (zAxis.sqrMagnitude < Constants.EPS)
            {
                hint = canalNodes[i - 1].transform.localRotation * Vector3.right; // fallback
                zAxis = hint - Vector3.Project(hint, yAxis);
            }

            zAxis.Normalize();

            OvalCylinderShape cyl = new(
                center,
                Quaternion.LookRotation(zAxis, yAxis),
                distVec.magnitude,
                canalNodes[i - 1].rx,
                canalNodes[i - 1].rz);

            if (prev == null)
            {
                prev = cyl;
            }
            else
            {
                // Sphere is used to smooth out points where cylinders meet
                SphereShape sphere = new(
                    a,
                    Quaternion.identity,
                    cyl.rz);

                UnionShape union0 = new(
                    Vector3.zero,
                    Quaternion.identity,
                    prev,
                    sphere);

                UnionShape union1 = new(
                    Vector3.zero,
                    Quaternion.identity,
                    union0,
                    cyl);

                prev = union1;
            }
        }
        return prev;
    }

    // Draws the bounds of an oval cylinder
    private void DrawCylinder(OvalCylinderShape shape)
    {
        // Get local positions
        Vector3 a = Vector3.up * shape.height / 2f;
        Vector3 b = -a;
        Vector3 xOffset = Vector3.right * shape.rx;
        Vector3 zOffset = Vector3.forward * shape.rz;

        // Get world positions
        a = shape.GetWorldPos(a);
        b = shape.GetWorldPos(b);
        xOffset = shape.GetWorldDir(xOffset);
        zOffset = shape.GetWorldDir(zOffset);

        // Drawing
        Gizmos.DrawLine(a, b);

        Gizmos.DrawLine(a + xOffset, a - xOffset);
        Gizmos.DrawLine(a + zOffset, a - zOffset);
        Gizmos.DrawLine(b + xOffset, b - xOffset);
        Gizmos.DrawLine(b + zOffset, b - zOffset);

        Gizmos.DrawLine(a + xOffset, b + xOffset);
        Gizmos.DrawLine(a - xOffset, b - xOffset);
        Gizmos.DrawLine(a + zOffset, b + zOffset);
        Gizmos.DrawLine(a - zOffset, b - zOffset);
    }

    // Recursively walks through the tree, drawing cylinders and spheres
    private void DrawShapeTree(CollisionShape curr)
    {
        if (curr is OvalCylinderShape oval)
        {
            DrawCylinder(oval);
            return;
        }
        if (curr is UnionShape union)
        {
            DrawShapeTree(union.a);
            DrawShapeTree(union.b);
            return;
        }
        if (curr is SphereShape sphere)
        {
            Vector3 center = sphere.GetWorldPos(Vector3.zero);
            Gizmos.DrawWireSphere(center, sphere.radius * this.transform.lossyScale.x);
            return;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        this.viewer = new(
            this.viewSize,
            this.viewResolution,
            this.viewParticleSize);
    }

    // Rebuilds shape tree and viewer
    public void Rebuild()
    {
        shape = BuildShapeTree();
        shape.RecurseSetup(this, null);
        this.viewer = new(
            this.viewSize,
            this.viewResolution,
            this.viewParticleSize);
    }

    private void OnValidate()
    {
        Rebuild();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        DrawShapeTree(this.shape);
        if (drawLattice) this.viewer.DrawLattice(this, viewCutoff);
    }
}
