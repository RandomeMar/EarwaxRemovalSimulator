using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;


namespace EarwaxSim
{
    // ------ Structs ------

    // Material properties used by collision objects
    public struct MaterialProperties
    {
        public float dynamicFriction;
        public float adhesCompliance;
        public float adhesBreakDist;
    }

    // Houses info returned from a collision query
    public struct CollisionInfo
    {
        public float signedDistance;
        public Vector3 collNormal;
        public CollisionShape owner;

        public CollisionInfo(float signedDistance, Vector3 collNormal, CollisionShape owner)
        {
            this.signedDistance = signedDistance;
            this.collNormal = collNormal;
            this.owner = owner;
        }
    }


    // ------ Collision Shape Viewers ------
    
    // Used for viewing a 2d slice of a collision shape
    public class ViewingSlice
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector2 size;
        public Vector2Int resolution;

        public float particleSize;

        public ViewingSlice(Vector3 position, Quaternion rotation, Vector2 size, Vector2Int resolution, float particleSize)
        {
            this.position = position;
            this.rotation = rotation;
            this.size = size;
            this.resolution = resolution;
            this.particleSize = particleSize;
        }

        public void DrawSlice(CollisionShape shape)
        {
            float xSpacing = this.size.x / (this.resolution.x - 1); // Length / (n - 1)
            float ySpacing = this.size.y / (this.resolution.y - 1);

            float xStart = -this.size.x / 2;
            float yStart = -this.size.y / 2;

            Gizmos.color = new(1f, 0f, 0f, .9f);

            for (int x = 0; x < this.resolution.x; x++)
            {

                for (int y = 0; y < this.resolution.y; y++)
                {
                    // Calculate the world coordinate of the sample
                    Vector3 localPos = new(xStart + xSpacing * x, yStart + ySpacing * y, 0f);
                    Vector3 globalPos = this.position + this.rotation * localPos;

                    // Send that coordinate to the GetSignedDistance function
                    float sd = shape.GetSignedDistancePoint(globalPos);

                    // Call DrawSphere()
                    if (sd <= 0f) Gizmos.DrawSphere(globalPos, this.particleSize);
                }
            }
        }
    }

    // Used for viewing the 3d volume of a collision shape
    public class ViewingLattice
    {
        public Vector3 size;
        public Vector3Int resolution;

        public float particleSize;

        public ViewingLattice(Vector3 size, Vector3Int resolution, float particleSize)
        {
            this.size = size;
            this.resolution = resolution;
            this.particleSize = particleSize;
        }

        public void DrawLattice(CollisionObjectBase obj, float cutoff)
        {
            float xSpacing = this.size.x / (this.resolution.x - 1); // Length / (n - 1)
            float ySpacing = this.size.y / (this.resolution.y - 1);
            float zSpacing = this.size.z / (this.resolution.z - 1);

            float xStart = -this.size.x / 2;
            float yStart = -this.size.y / 2;
            float zStart = -this.size.z / 2;

            Gizmos.color = new(1f, 0f, 0f, .9f);
            Gizmos.matrix = obj.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, this.size);
            Gizmos.matrix = Matrix4x4.identity;

            for (int x = 0; x < this.resolution.x; x++)
            {
                for (int y = 0; y < this.resolution.y; y++)
                {
                    for (int z = 0; z < this.resolution.z; z++)
                    {
                        Vector3 localPos = new(xStart + xSpacing * x, yStart + ySpacing * y, zStart + zSpacing * z);
                        Vector3 globalPos = obj.transform.TransformPoint(localPos);

                        // Send that coordinate to the GetSignedDistance function
                        float sd = obj.GetSignedDistance(globalPos, 0f);

                        // Draw lattice particle
                        if (Mathf.Abs(sd) <= cutoff) Gizmos.DrawCube(globalPos, Vector3.one * this.particleSize);
                    }
                }
            }
        }
    }


    // ------ Collision Shapes ------

    // Collision shapes only define shape
    public abstract class CollisionShape
    {
        public Vector3 position;
        public Quaternion rotation;
        public CollisionObjectBase owner;
        public CollisionShape parent;
        

        protected CollisionShape(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        public Vector3 GetLocalPos(Vector3 worldPos)
        {
            Stack<CollisionShape> stack = new();
            CollisionShape curr = this;
            
            // Add parent nodes to the stack
            while (curr != null)
            {
                stack.Push(curr);
                curr = curr.parent;
            }

            Vector3 localPos = this.owner.transform.InverseTransformPoint(worldPos);

            // Traverse the tree down from the root
            while (stack.Count > 0)
            {
                CollisionShape node = stack.Pop();

                localPos = Quaternion.Inverse(node.rotation) * (localPos - node.position);
            }

            return localPos;
        }

        public Vector3 GetWorldPos(Vector3 localPos)
        {
            CollisionShape curr = this;
            Vector3 ownerLocalPos = localPos;

            // Traverse the tree up to the root
            while (curr != null)
            {
                ownerLocalPos = curr.rotation * ownerLocalPos + curr.position;
                curr = curr.parent;
            }

            return 
               this.owner.transform.TransformPoint(ownerLocalPos);
        }

        public Vector3 GetWorldDir(Vector3 localDir)
        {
            CollisionShape curr = this;
            Vector3 ownerLocalDir = localDir;

            // Traverse the tree up to the root
            while (curr != null)
            {
                ownerLocalDir = curr.rotation * ownerLocalDir;
                curr = curr.parent;
            }

            return this.owner.transform.TransformVector(ownerLocalDir);
        }

        public CollisionInfo GetCollisionInfoPoint(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position); // Convert particle position to local space

            CollisionInfo localHit = GetCollisionInfoLocal(pLocal);

            localHit.collNormal = (this.rotation * localHit.collNormal).normalized; // Convert collision normal to parent node's space
            return localHit;
        }

        public float GetSignedDistancePoint(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position); // Convert particle position to local space
            return this.GetSignedDistanceLocal(pLocal);
        }

        public virtual void RecurseSetup(CollisionObjectBase owner, CollisionShape parent)
        {
            this.owner = owner;
            this.parent = parent;
            return;
        }


        protected abstract CollisionInfo GetCollisionInfoLocal(Vector3 pLocal);
        protected abstract float GetSignedDistanceLocal(Vector3 particlePos);
    }

    // Defines plane collision shape
    public class PlaneShape : CollisionShape
    {
        public Vector3 normal;


        public PlaneShape(Vector3 position, Quaternion rotation) : base(position, rotation) 
        {
            this.normal = Vector3.up;
        }

        public PlaneShape(Vector3 position, Vector3 rotationEuler) : this(position, Quaternion.Euler(rotationEuler))
        {
        }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            CollisionInfo collisionInfo = new(Vector3.Dot(this.normal, pLocal), this.normal, this);
            return collisionInfo;
        }

        protected override float GetSignedDistanceLocal(Vector3 pLocal)
        {
            return Vector3.Dot(this.normal, pLocal);
        }
    }

    // Defines sphere collision shape
    public class SphereShape : CollisionShape
    {
        public float radius;

        public SphereShape(Vector3 position, Quaternion rotation, float radius) : base(position, rotation)
        {
            this.radius = radius;
        }
        public SphereShape(Vector3 position, Vector3 rotationEuler, float radius) : this(position, Quaternion.Euler(rotationEuler), radius)
        {
        }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            Vector3 distVec = pLocal - Vector3.zero;

            if (distVec == Vector3.zero) return new CollisionInfo(-this.radius, Vector3.up, this); // Fallback in case particle is at sphere center

            return new CollisionInfo(distVec.magnitude - this.radius, distVec, this);
        }

        protected override float GetSignedDistanceLocal(Vector3 pLocal)
        {
            Vector3 distVec = pLocal - Vector3.zero;
            return distVec.magnitude - radius;
        }
    }

    // Defines capsule collision shape
    public class CapsuleShape : CollisionShape
    {
        public float height;
        public float radius;

        public Vector3 a;
        public Vector3 b;

        // Cached values
        private Vector3 ba;
        private float baSqrMag;

        public CapsuleShape(Vector3 position, Quaternion rotation, float height, float radius) : base(position, rotation)
        {
            this.height = height;
            this.radius = radius;

            this.a = new Vector3(0f, height / 2f - radius, 0f);
            this.b = new Vector3(0f, -height / 2f + radius, 0f);
            this.ba = b - a;
            this.baSqrMag = Vector3.Dot(this.ba, this.ba);
        }
        public CapsuleShape(Vector3 position, Vector3 rotationEuler, float height, float radius) : this(position, Quaternion.Euler(rotationEuler), height, radius) { }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            float t = Vector3.Dot((pLocal - this.a), ba) / this.baSqrMag;
            t = Mathf.Clamp(t, 0f, 1f); // 0 = a, 1 = b

            Vector3 q = this.a + t * this.ba; // Position of closest point on line segment a to b

            Vector3 normal = (pLocal - q);
            float signedDistance = normal.magnitude - this.radius;

            CollisionInfo collisionInfo = new(
                signedDistance,
                normal,
                this);

            return collisionInfo;
        }

        protected override float GetSignedDistanceLocal(Vector3 pLocal)
        {
            float t = Vector3.Dot((pLocal - this.a), ba) / this.baSqrMag;
            t = Mathf.Clamp(t, 0f, 1f); // 0 = a, 1 = b

            Vector3 q = this.a + t * this.ba; // Position of closest point on line segment a to b

            Vector3 normal = (pLocal - q);
            return normal.magnitude - this.radius;
        }
    }

    // Defines box collision shape
    public class BoxShape : CollisionShape
    {
        public Vector3 b; // half-extents


        public BoxShape(Vector3 center, Quaternion rotation, Vector3 b) : base(center, rotation)
        {
            this.b = b;
        }
        public BoxShape(Vector3 center, Vector3 rotationEuler, Vector3 b) : this(center, Quaternion.Euler(rotationEuler), b) { }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            Vector3 sign = new(
                pLocal.x >= 0f ? 1f : -1f,
                pLocal.y >= 0f ? 1f : -1f,
                pLocal.z >= 0f ? 1f : -1f
                );

            Vector3 q = new Vector3(
                Mathf.Abs(pLocal.x),
                Mathf.Abs(pLocal.y),
                Mathf.Abs(pLocal.z)
                ) - this.b;

            Vector3 outside = new(
                Mathf.Max(q.x, 0f),
                Mathf.Max(q.y, 0f),
                Mathf.Max(q.z, 0f)
                );

            // Calculate signed distance
            float outsideDist = outside.magnitude;
            float insideDist = Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0f);
            float signedDistance = outsideDist + insideDist;

            Vector3 collNormal;

            // If particle is outside the box
            if (outside.sqrMagnitude > Constants.EPS)
            {
                collNormal = Vector3.Scale(outside.normalized, sign);
            }
            // If particle is inside the box, return the normal of the nearest face
            else if (q.x > q.y && q.x > q.z) collNormal = new Vector3(sign.x, 0f, 0f);
            else if (q.y > q.z) collNormal = new Vector3(0f, sign.y, 0f);
            else collNormal = new Vector3(0f, 0f, sign.z);

            return new CollisionInfo(signedDistance, collNormal, this);
        }

        protected override float GetSignedDistanceLocal(Vector3 pLocal)
        {
            Vector3 q = new Vector3(
                Mathf.Abs(pLocal.x),
                Mathf.Abs(pLocal.y),
                Mathf.Abs(pLocal.z)
                ) - this.b;

            Vector3 outside = new(
                Mathf.Max(q.x, 0f),
                Mathf.Max(q.y, 0f),
                Mathf.Max(q.z, 0f)
                );

            float outsideDist = outside.magnitude;
            float insideDist = Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0f);
            return outsideDist + insideDist;
        }
    }

    // Defines torus collision shape
    public class TorusShape : CollisionShape
    {
        public float rMajor;
        public float rMinor;


        public TorusShape(Vector3 position, Quaternion rotation, float rMajor, float rMinor) : base(position, rotation)
        {
            this.rMajor = rMajor;
            this.rMinor = rMinor;
        }
        public TorusShape(Vector3 position, Vector3 rotationEuler, float rMajor, float rMinor) : this(position, Quaternion.Euler(rotationEuler), rMajor, rMinor) { }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            float radial = Mathf.Sqrt(pLocal.x * pLocal.x + pLocal.z * pLocal.z); // Magnitude of particles position on the x, z plane
            Vector2 q = new(radial - this.rMajor, pLocal.y); // Assuming the closest point on the major axis to pLocal is at (0, 0), this vector points from it to the particle.

            float signedDistance = q.magnitude - this.rMinor;

            Vector3 collNormal;
            if (radial > Constants.EPS)
            {
                Vector3 ringCenter = new(
                    this.rMajor * pLocal.x / radial,
                    0f,
                    this.rMajor * pLocal.z / radial
                    ); // This vector is the point on the major axis that pLocal is closest to

                collNormal = pLocal - ringCenter;

                collNormal = collNormal.sqrMagnitude > Constants.EPS ? collNormal.normalized : Vector3.up;
            }
            else collNormal = Vector3.up;

            return new CollisionInfo(signedDistance, collNormal, this);
        }

        protected override float GetSignedDistanceLocal(Vector3 pLocal)
        {
            Vector2 q = new(new Vector2(pLocal.x, pLocal.z).magnitude - this.rMajor, pLocal.y);
            return q.magnitude - this.rMinor;
        }
    }

    // Defines an oval cross section cylinder collision shape
    public class OvalCylinderShape : CollisionShape
    {
        public float height;
        public float rx;
        public float rz;

        // Cached values
        protected float rx2;
        protected float rz2;
        protected float halfHeight;
        

        public OvalCylinderShape(Vector3 position, Quaternion rotation, float height, float rx, float rz) : base(position, rotation)
        {
            this.height = height;
            this.rx = rx;
            this.rz = rz;

            this.rx2 = rx * rx;
            this.rz2 = rz * rz;
            this.halfHeight = height / 2f;
        }
        public OvalCylinderShape(Vector3 position, Vector3 rotationEuler, float height, float rx, float rz) : this(position, Quaternion.Euler(rotationEuler), height, rx, rz) { }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            float dy = Mathf.Abs(pLocal.y) - this.halfHeight;
            float k = Mathf.Sqrt((pLocal.x * pLocal.x) / this.rx2 + (pLocal.z * pLocal.z) / this.rz2);

            float minRadius = Mathf.Min(this.rx, this.rz);
            float sideDist = (k - 1f) * minRadius; // Aproximate side distance


            Vector3 sideNorm; // Sideways normal component
            {
                Vector3 grad = new Vector3(
                    pLocal.x / this.rx2,
                    0f,
                    pLocal.z / this.rz2
                );

                sideNorm = (grad.sqrMagnitude <= Constants.EPS) ? Vector3.right : grad.normalized;
            }

            // Cap normal
            Vector3 capNorm = (pLocal.y >= 0f) ? Vector3.up : Vector3.down;


            float signedDist;
            Vector3 normal;

            // Outside corner
            if (sideDist > 0 && dy > 0)
            {
                signedDist = Mathf.Sqrt(sideDist * sideDist + dy * dy);

                // Blend side and cap normals
                Vector3 corner = sideNorm * sideDist + capNorm * dy;

                normal = (corner.sqrMagnitude <= Constants.EPS) ? capNorm : corner.normalized;
            }
            // Outside side only
            else if (sideDist > 0)
            {
                signedDist = sideDist;
                normal = sideNorm;
            }
            // Outside cap only
            else if (dy > 0)
            {
                signedDist = dy;
                normal = capNorm;
            }
            // Inside cylinder
            else
            {
                signedDist = Mathf.Max(sideDist, dy);
                normal = (sideDist > dy) ? sideNorm : capNorm;
            }

            return new CollisionInfo(signedDist, normal, this);
        }

        protected override float GetSignedDistanceLocal(Vector3 pLocal)
        {
            float dy = Mathf.Abs(pLocal.y) - this.halfHeight;
            float k = Mathf.Sqrt((pLocal.x * pLocal.x) / this.rx2 + (pLocal.z * pLocal.z) / this.rz2);

            float minRadius = Mathf.Min(this.rx, this.rz);
            float sideDist = (k - 1f) * minRadius; // Aproximate side distance


            float signedDist;

            // Outside corner
            if (sideDist > 0 && dy > 0) signedDist = Mathf.Sqrt(sideDist * sideDist + dy * dy);
            // Outside side only
            else if (sideDist > 0) signedDist = sideDist;
            // Outside cap only
            else if (dy > 0) signedDist = dy;
            // Inside cylinder
            else signedDist = Mathf.Max(sideDist, dy);

            return signedDist;
        }
    }


    // ------ Boolean Operations ------

    // Class defining boolean operations performed on collision shapes
    public abstract class BooleanShape : CollisionShape
    {
        protected BooleanShape(Vector3 position, Quaternion rotation) : base(position, rotation)
        {
        }

        protected Vector3 EstimateNormal(Vector3 pLocal)
        {
            float e = .001f;

            // This estimates the gradient of the union at positon particlePos
            float dx = this.GetSignedDistancePoint(pLocal + new Vector3(e, 0, 0))
                - this.GetSignedDistancePoint(pLocal - new Vector3(e, 0, 0));

            float dy = this.GetSignedDistancePoint(pLocal + new Vector3(0, e, 0))
                - this.GetSignedDistancePoint(pLocal - new Vector3(0, e, 0));

            float dz = this.GetSignedDistancePoint(pLocal + new Vector3(0, 0, e))
                - this.GetSignedDistancePoint(pLocal - new Vector3(0, 0, e));

            return new Vector3(dx, dy, dz).normalized;
        }

        protected abstract void AssignParents();
    }

    // Unions two collision shapes
    public class UnionShape : BooleanShape
    {
        public CollisionShape a;
        public CollisionShape b;

        public UnionShape(Vector3 position, Quaternion rotation, CollisionShape a, CollisionShape b) : base(position, rotation)
        {
            this.a = a;
            this.b = b;
            this.AssignParents();
        }
        public UnionShape(Vector3 position, Vector3 rotationEuler, CollisionShape a, CollisionShape b) : this(position, Quaternion.Euler(rotationEuler), a, b) { }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            CollisionInfo aColl = this.a.GetCollisionInfoPoint(pLocal);
            CollisionInfo bColl = this.b.GetCollisionInfoPoint(pLocal);

            float aDist = aColl.signedDistance;
            float bDist = bColl.signedDistance;

            if (Mathf.Abs(aDist - bDist) > Constants.SEAM_EPS) return aDist < bDist ? aColl : bColl;

            CollisionInfo collisionInfo = new(
                Mathf.Min(aDist, bDist),
                this.EstimateNormal(pLocal),
                aColl.owner);

            return collisionInfo;
        }

        protected override float GetSignedDistanceLocal(Vector3 pLocal)
        {
            float aDist = this.a.GetSignedDistancePoint(pLocal);
            float bDist = this.b.GetSignedDistancePoint(pLocal);
            return Mathf.Min(aDist, bDist);
        }

        public override void RecurseSetup(CollisionObjectBase owner, CollisionShape parent)
        {
            base.RecurseSetup(owner, parent);
            a.RecurseSetup(owner, this);
            b.RecurseSetup(owner, this);
            return;
        }

        protected override void AssignParents()
        {
            this.a.parent = this;
            this.b.parent = this;
        }
    }

    // Intersects two collision shapes
    public class IntersectShape : BooleanShape
    {
        public CollisionShape a;
        public CollisionShape b;

        public IntersectShape(Vector3 position, Quaternion rotation, CollisionShape a, CollisionShape b) : base(position, rotation)
        {
            this.a = a;
            this.b = b;
            this.AssignParents();
        }
        public IntersectShape(Vector3 position, Vector3 rotationEuler, CollisionShape a, CollisionShape b) : this(position, Quaternion.Euler(rotationEuler), a, b) { }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            CollisionInfo aColl = this.a.GetCollisionInfoPoint(pLocal);
            CollisionInfo bColl = this.b.GetCollisionInfoPoint(pLocal);

            float aDist = aColl.signedDistance;
            float bDist = bColl.signedDistance;

            if (Mathf.Abs(aDist - bDist) > Constants.SEAM_EPS) return aDist > bDist ? aColl : bColl;

            CollisionInfo collisionInfo = new(
                Mathf.Max(aDist, bDist),
                this.EstimateNormal(pLocal),
                aColl.owner);

            return collisionInfo;
        }

        protected override float GetSignedDistanceLocal(Vector3 pLocal)
        {
            float aDist = this.a.GetSignedDistancePoint(pLocal);
            float bDist = this.b.GetSignedDistancePoint(pLocal);
            return Mathf.Max(aDist, bDist);
        }

        public override void RecurseSetup(CollisionObjectBase owner, CollisionShape parent)
        {
            base.RecurseSetup(owner, parent);
            a.RecurseSetup(owner, this);
            b.RecurseSetup(owner, this);
            return;
        }

        protected override void AssignParents()
        {
            this.a.parent = this;
            this.b.parent = this;
        }
    }

    // Subtracts shape b from a: a - b
    public class DifferenceShape : BooleanShape
    {
        public CollisionShape a;
        public CollisionShape b;

        public DifferenceShape(Vector3 position, Quaternion rotation, CollisionShape a, CollisionShape b) : base(position, rotation)
        {
            this.a = a;
            this.b = b;
            this.AssignParents();
        }
        public DifferenceShape(Vector3 position, Vector3 rotationEuler, CollisionShape a, CollisionShape b) : this(position, Quaternion.Euler(rotationEuler), a, b) { }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            CollisionInfo aColl = this.a.GetCollisionInfoPoint(pLocal);
            CollisionInfo bColl = this.b.GetCollisionInfoPoint(pLocal);
            bColl.signedDistance *= -1;
            bColl.collNormal *= -1;

            float aDist = aColl.signedDistance;
            float bDist = bColl.signedDistance;

            if (Mathf.Abs(aDist - bDist) > Constants.SEAM_EPS) return aDist > bDist ? aColl : bColl;

            CollisionInfo collisionInfo = new(
                Mathf.Max(aDist, bDist),
                this.EstimateNormal(pLocal),
                aColl.owner);

            return collisionInfo;
        }

        protected override float GetSignedDistanceLocal(Vector3 pLocal)
        {
            float aDist = this.a.GetSignedDistancePoint(pLocal);
            float bDist = this.b.GetSignedDistancePoint(pLocal);
            return Mathf.Max(aDist, -bDist);
        }

        public override void RecurseSetup(CollisionObjectBase owner, CollisionShape parent)
        {
            base.RecurseSetup(owner, parent);
            a.RecurseSetup(owner, this);
            b.RecurseSetup(owner, this);
            return;
        }

        protected override void AssignParents()
        {
            this.a.parent = this;
            this.b.parent = this;
        }
    }

    // Inverses a collision shape
    public class InverseShape : BooleanShape
    {
        public CollisionShape shape;

        public InverseShape(Vector3 position, Quaternion rotation, CollisionShape shape) : base(position, rotation)
        {
            this.shape = shape;
            this.AssignParents();
        }
        public InverseShape(Vector3 position, Vector3 rotationEuler, CollisionShape shape) : this(position, Quaternion.Euler(rotationEuler), shape) { }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            CollisionInfo collisionInfo = shape.GetCollisionInfoPoint(pLocal);
            collisionInfo.signedDistance *= -1;
            collisionInfo.collNormal *= -1;
            return collisionInfo;
        }

        protected override float GetSignedDistanceLocal(Vector3 pLocal)
        {
            return -shape.GetSignedDistancePoint(pLocal);
        }

        public override void RecurseSetup(CollisionObjectBase owner, CollisionShape parent)
        {
            base.RecurseSetup(owner, parent);
            shape.RecurseSetup(owner, this);
            return;
        }

        protected override void AssignParents()
        {
            this.shape.parent = this;
        }
    }


    // ------ Collision Objects (DEPRICATED) -------

    //// Object with a defined shape, position, rotation, and material properties like friction. Stored inside collision constraint.
    //public class CollisionObject : CollisionShape
    //{
    //    public CollisionShape shape;

    //    public MaterialProperties matProps;

    //    public CollisionObject(Vector3 position, Quaternion rotation, CollisionShape shape, MaterialProperties matProps) : base(position, rotation)
    //    {
    //        this.shape = shape;
    //        this.shape.parent = this;

    //        this.matProps = matProps;
    //    }


    //    protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
    //    {
    //        return this.shape.GetCollisionInfo(pLocal);
    //    }

    //    protected override float GetSignedDistanceLocal(Vector3 pLocal)
    //    {
    //        return this.shape.GetSignedDistance(pLocal);
    //    }
    //}

    //// Collision object that is infuenced by particles
    //public class DynamicCollisionObject : CollisionObject
    //{
    //    public Vector3 previousPosition;
    //    public Vector3 targetPosition;

    //    public Vector3 velocity;
    //    public float invMass;

    //    public DynamicCollisionObject(Vector3 position, Quaternion rotation, CollisionShape shape, MaterialProperties matProps, float invMass) : base(position, rotation, shape, matProps)
    //    {
    //        this.invMass = invMass;
    //        this.previousPosition = this.position;
    //        this.velocity = Vector3.zero;
    //    }

    //    public void PredictPosition()
    //    {
    //        this.position = this.targetPosition;
    //    }
    //}


}
