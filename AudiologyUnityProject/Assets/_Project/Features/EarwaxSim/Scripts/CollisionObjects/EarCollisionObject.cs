using EarwaxSim;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
//using UnityEditor.Compilation;
using UnityEngine;

/// <summary>
/// Collision object representing the ear.
/// </summary>
public class EarCollisionObject : CollisionObjectBase
{
    [Header("Collider Node Guides")]
    public List<CanalNode> canalNodes = new List<CanalNode>(5);
    public CanalNode[] conchaNodes = new CanalNode[2];
    public List<CanalNode> tragusNodes = new List<CanalNode>(6);

    [Header("Viewer Settings")]
    public bool drawLattice = true;
    public Vector3 viewSize;
    public Vector3Int viewResolution;
    [Min(0f)]
    public float viewParticleSize = .1f;
    [Min(0f)]
    public float viewCutoff = .1f;

    BoxCollider boxCollider;
    ViewingLattice viewer;

    /// <summary>
    /// Builds the SDF based CollisionShape tree.
    /// </summary>
    /// <returns>Root node of the CollisionShape tree.</returns>
    /// <remarks>
    /// In order to build the shape tree, this method utilizes lists of canal nodes in order to define the shape canal.
    /// In order to edit the shape, the canal nodes must be moved in editor.
    /// </remarks>
    protected override CollisionShape BuildShapeTree()
    {
        if (canalNodes == null)
        {
            Debug.LogError("canalPoints is null");
        }

        List<CollisionShape> negatorList = new(4);
        List<CollisionShape> adderList = new(4);

        // ------ Negators ------
        // Build canal collider
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

            negatorList.Add(new OvalCylinderShape(
                center,
                Quaternion.LookRotation(zAxis, yAxis),
                distVec.magnitude,
                canalNodes[i - 1].rx,
                canalNodes[i - 1].rz));

            if (i > 1)
            {
                // Sphere is used to smooth out points where cylinders meet
                negatorList.Add(new SphereShape(
                    a,
                    Quaternion.identity,
                    canalNodes[i - 1].rz));
            }
        }

        // Build concha collider
        OvalCylinderShape concha;
        {
            Vector3 a = conchaNodes[0].transform.localPosition;
            Vector3 b = conchaNodes[1].transform.localPosition;

            Vector3 distVec = b - a;
            Vector3 center = (a + b) / 2f;

            Vector3 yAxis = distVec.normalized; // Cylinders fall along the y-axis
            Vector3 hint = conchaNodes[0].transform.localRotation * Vector3.forward; // CanalNode's local y rotation controls the roll of the cylinder
            Vector3 zAxis = hint - Vector3.Project(hint, yAxis);

            if (zAxis.sqrMagnitude < Constants.EPS)
            {
                hint = conchaNodes[0].transform.localRotation * Vector3.right; // fallback
                zAxis = hint - Vector3.Project(hint, yAxis);
            }

            zAxis.Normalize();

            concha = new(
                center,
                Quaternion.LookRotation(zAxis, yAxis),
                distVec.magnitude,
                conchaNodes[0].rx,
                conchaNodes[0].rz);
        }

        negatorList.Add(concha);

        var negators = BuildBalancedUnion(negatorList);


        // ------ Adders ------
        // Build tragus collider
        for (int i = 1; i < tragusNodes.Count; i++)
        {
            Vector3 a = tragusNodes[i - 1].transform.localPosition;
            Vector3 b = tragusNodes[i].transform.localPosition;
            Vector3 distVec = b - a;
            Vector3 center = (a + b) / 2f;

            Vector3 yAxis = distVec.normalized; // Cylinders fall along the y-axis
            Vector3 hint = tragusNodes[i - 1].transform.localRotation * Vector3.forward; // CanalNode's local y rotation controls the roll of the cylinder
            Vector3 zAxis = hint - Vector3.Project(hint, yAxis);

            if (zAxis.sqrMagnitude < Constants.EPS)
            {
                hint = tragusNodes[i - 1].transform.localRotation * Vector3.right; // fallback
                zAxis = hint - Vector3.Project(hint, yAxis);
            }

            zAxis.Normalize();

            adderList.Add(new CapsuleShape(
                center,
                Quaternion.LookRotation(zAxis, yAxis),
                distVec.magnitude,
                tragusNodes[i - 1].rx));
        }

        BoxShape box = new(
            boxCollider.center,
            Quaternion.identity,
            boxCollider.size / 2f);

        adderList.Add(box);

        var adders = BuildBalancedUnion(adderList);

        DifferenceShape diff = new(
            Vector3.zero,
            Quaternion.identity,
            adders,
            negators); // Final collider

        return diff;
    }

    /// <summary>
    /// Builds balanced union shape trees.
    /// </summary>
    /// <param name="shapes">List of shape primitives to union together in a balanced tree.</param>
    /// <returns>Root node of the balanced union CollisionShape tree.</returns>
    private CollisionShape BuildBalancedUnion(List<CollisionShape> shapes)
    {
        if (shapes == null || shapes.Count == 0) return null;
        if (shapes.Count == 1) return shapes[0];

        List<CollisionShape> curr = shapes;

        while (curr.Count > 1)
        {
            List<CollisionShape> next = new();

            for (int i = 0; i < curr.Count; i += 2)
            {
                if (i + 1 < curr.Count)
                {
                    next.Add(new UnionShape(
                        Vector3.zero,
                        Quaternion.identity,
                        curr[i],
                        curr[i + 1]));
                }
                else
                {
                    next.Add(curr[i]);
                }
            }

            curr = next;
        }

        return curr[0];
    }


    /// <summary>
    /// Draws the bounds of an oval cylinder.
    /// </summary>
    /// <param name="shape">Shape to be drawn.</param>
    /// <remarks>This method should only be called from OnDrawGizmos.</remarks>
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

    /// <summary>
    /// Draws the bounds of a capsule.
    /// </summary>
    /// <param name="shape">Shape to be drawn.</param>
    /// <remarks>This method should only be called from OnDrawGizmos.</remarks>
    private void DrawCapsule(CapsuleShape shape)
    {
        // Get local positions
        Vector3 a = Vector3.up * shape.height / 2f;
        Vector3 b = -a;
        Vector3 xOffset = Vector3.right * shape.radius;
        Vector3 zOffset = Vector3.forward * shape.radius;

        // Get world positions
        a = shape.GetWorldPos(a);
        b = shape.GetWorldPos(b);
        xOffset = shape.GetWorldDir(xOffset);
        zOffset = shape.GetWorldDir(zOffset);

        // Drawing
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(a, b);

        Gizmos.DrawWireSphere(a, shape.radius * this.transform.lossyScale.x);
        Gizmos.DrawWireSphere(b, shape.radius * this.transform.lossyScale.x);

        Gizmos.DrawLine(a + xOffset, a - xOffset);
        Gizmos.DrawLine(a + zOffset, a - zOffset);
        Gizmos.DrawLine(b + xOffset, b - xOffset);
        Gizmos.DrawLine(b + zOffset, b - zOffset);

        Gizmos.DrawLine(a + xOffset, b + xOffset);
        Gizmos.DrawLine(a - xOffset, b - xOffset);
        Gizmos.DrawLine(a + zOffset, b + zOffset);
        Gizmos.DrawLine(a - zOffset, b - zOffset);
    }

    /// <summary>
    /// Recursively walks through the tree, drawing collision shapes.
    /// </summary>
    /// <remarks>This method should only be called from OnDrawGizmos.</remarks>
    /// <param name="curr">The current node being visited.</param>
    private void DrawShapeTree(CollisionShape curr)
    {
        Gizmos.color = Color.yellow;
        // Non-leaf nodes
        if (curr is UnionShape union)
        {
            DrawShapeTree(union.a);
            DrawShapeTree(union.b);
            return;
        }
        if (curr is DifferenceShape diff)
        {
            DrawShapeTree(diff.a);
            DrawShapeTree(diff.b);
            return;
        }

        // Leaf nodes
        if (curr is OvalCylinderShape oval)
        {
            DrawCylinder(oval);
            return;
        }
        if (curr is SphereShape sphere)
        {
            Vector3 center = sphere.GetWorldPos(Vector3.zero);
            Gizmos.DrawWireSphere(center, sphere.radius * this.transform.lossyScale.x);
            return;
        }
        if (curr is BoxShape box)
        {
            Color col = Color.blue;
            col.a = .25f;
            Gizmos.color = col;

            Gizmos.matrix = this.transform.localToWorldMatrix;
            Gizmos.DrawCube(this.boxCollider.center, this.boxCollider.size);
            Gizmos.matrix = Matrix4x4.identity;
            return;
        }
        if (curr is CapsuleShape cap)
        {
            DrawCapsule(cap);
            return;
        }
    }

    protected override void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        base.Awake();
        this.viewer = new(
            this.viewSize,
            this.viewResolution,
            this.viewParticleSize);
    }

    // Rebuilds shape tree and viewer
    public void Rebuild()
    {
        boxCollider = GetComponent<BoxCollider>();
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
