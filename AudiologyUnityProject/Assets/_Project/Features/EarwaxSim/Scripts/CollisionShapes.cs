using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace EarwaxSim
{
    // Object with a defined shape and material properties like friction. Stored inside collision constraint.
    public class CollisionObject
    {
        public CollisionShape shape;

        public float dynamicFriction;
        public float adhesCompliance;
        public float adhesBreakDist;

        public CollisionObject(CollisionShape shape, float dynamicFriction, float adhesCompliance, float adhesBreakDist)
        {
            this.shape = shape;

            this.dynamicFriction = dynamicFriction;
            this.adhesCompliance = adhesCompliance;
            this.adhesBreakDist = adhesBreakDist;
        }

        public CollisionInfo GetCollisionInfo(Vector3 particlePos)
        {
            return this.shape.GetCollisionInfo(particlePos);
        }
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

    // Used for viewing collision shapes
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
                    float sd = shape.GetSignedDistance(globalPos);

                    // Call DrawSphere()
                    if (sd <= 0f) Gizmos.DrawSphere(globalPos, this.particleSize);
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
        public CollisionShape parent;

        protected CollisionShape(Vector3 position, Vector3 rotation)
        {
            this.position = position;
            this.rotation = Quaternion.Euler(rotation);
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

            Vector3 localPos = worldPos;

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
            Vector3 worldPos = localPos;

            // Traverse the tree up to the root
            while (curr != null)
            {
                worldPos = curr.rotation * worldPos + curr.position;
                curr = curr.parent;
            }

            return worldPos;
        }

        public Vector3 GetWorldDir(Vector3 localDir)
        {
            CollisionShape curr = this;
            Vector3 worldDir = localDir;

            // Traverse the tree up to the root
            while (curr != null)
            {
                worldDir = curr.rotation * worldDir;
                curr = curr.parent;
            }

            return worldDir;
        }

        public CollisionInfo GetCollisionInfo(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position); // Convert particle position to local space

            CollisionInfo localHit = GetCollisionInfoLocal(pLocal);

            localHit.collNormal = (this.rotation * localHit.collNormal).normalized; // Convert collision normal to parent node's space
            return localHit;
        }

        protected abstract CollisionInfo GetCollisionInfoLocal(Vector3 pLocal);
        public abstract float GetSignedDistance(Vector3 particlePos);
    }

    // Defines plane collision shape
    public class PlaneShape : CollisionShape
    {
        public Vector3 normal;


        public PlaneShape(Vector3 position, Vector3 rotation) : base(position, rotation) 
        {
            this.normal = Vector3.up;
        }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            CollisionInfo collisionInfo = new(Vector3.Dot(this.normal, pLocal), this.normal, this);
            return collisionInfo;
        }

        public override float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = this.GetLocalPos(particlePos);
            return Vector3.Dot(this.normal, pLocal);
        }
    }

    // Defines sphere collision shape
    public class SphereShape : CollisionShape
    {
        public float radius;

        public SphereShape(Vector3 position, Vector3 rotation, float radius) : base(position, rotation)
        {
            this.radius = radius;
        }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            Vector3 distVec = pLocal - Vector3.zero;

            if (distVec == Vector3.zero) return new CollisionInfo(-this.radius, Vector3.up, this); // Fallback in case particle is at sphere center

            return new CollisionInfo(distVec.magnitude - this.radius, distVec, this);
        }

        public override float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = this.GetLocalPos(particlePos);

            Vector3 distVec = pLocal - Vector3.zero;
            return distVec.magnitude - radius;
        }
    }

    //// Defines capsule collision shape NOTE: Rotation and position have not been implemented yet
    //// WARNING: DOES NOT WORK RIGHT NOW
    //public class CapsuleShapeOld : ICollisionShape
    //{
    //    public Vector3 a;
    //    public Vector3 b;
    //    public float radius;

    //    // Cached values
    //    private Vector3 ba;
    //    private float baSqrMag;

    //    public CapsuleShapeOld(Vector3 a, Vector3 b, float radius)
    //    {
    //        this.a = a;
    //        this.b = b;
    //        this.ba = b - a;
    //        this.baSqrMag = Vector3.Dot(this.ba, this.ba);
    //        this.radius = radius;
    //    }

    //    public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
    //    {
    //        float t = Vector3.Dot((particlePos - this.a), ba) / this.baSqrMag;
    //        t = Mathf.Clamp(t, 0f, 1f); // 0 = a, 1 = b

    //        Vector3 q = this.a + t * this.ba; // Position of closest point on line segment a to b

    //        Vector3 normal = (particlePos - q);
    //        float signedDistance = normal.magnitude - this.radius;

    //        return (signedDistance, normal.normalized);
    //    }

    //    public float GetSignedDistance(Vector3 particlePos)
    //    {
    //        float t = Vector3.Dot((particlePos - this.a), ba) / this.baSqrMag;
    //        t = Mathf.Clamp(t, 0f, 1f); // 0 = a, 1 = b

    //        Vector3 q = this.a + t * this.ba; // Position of closest point on line segment a to b

    //        Vector3 normal = (particlePos - q);
    //        return normal.magnitude - this.radius;
    //    }
    //}

    public class CapsuleShape : CollisionShape
    {
        public float spineLength;
        public float radius;

        public Vector3 a;
        public Vector3 b;

        // Cached values
        private Vector3 ba;
        private float baSqrMag;

        public CapsuleShape(Vector3 position, Vector3 rotation, float spineLength, float radius) : base(position, rotation)
        {
            this.spineLength = spineLength;
            this.radius = radius;

            this.a = new Vector3(0f, spineLength / 2f, 0f);
            this.b = new Vector3(0f, -spineLength / 2f, 0f);
            this.ba = b - a;
            this.baSqrMag = Vector3.Dot(this.ba, this.ba);
        }

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

        public override float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);

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


        public BoxShape(Vector3 center, Vector3 rotation, Vector3 b) : base(center, rotation)
        {
            this.b = b;
        }

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

        public override float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = this.GetLocalPos(particlePos);

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

    public class TorusShape : CollisionShape
    {
        public float rMajor;
        public float rMinor;


        public TorusShape(Vector3 position, Vector3 rotation, float rMajor, float rMinor) : base(position, rotation)
        {
            this.rMajor = rMajor;
            this.rMinor = rMinor;
        }

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

        public override float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = this.GetLocalPos(particlePos);

            Vector2 q = new(new Vector2(pLocal.x, pLocal.z).magnitude - this.rMajor, pLocal.y);
            return q.magnitude - this.rMinor;
        }
    }


    // ------ Boolean Operations ------

    public abstract class BooleanShape : CollisionShape
    {
        protected BooleanShape(Vector3 position, Vector3 rotation) : base(position, rotation)
        {
        }

        protected Vector3 EstimateNormal(Vector3 pLocal)
        {
            float e = .001f;

            // This estimates the gradient of the union at positon particlePos
            float dx = this.GetSignedDistance(pLocal + new Vector3(e, 0, 0))
                - this.GetSignedDistance(pLocal - new Vector3(e, 0, 0));

            float dy = this.GetSignedDistance(pLocal + new Vector3(0, e, 0))
                - this.GetSignedDistance(pLocal - new Vector3(0, e, 0));

            float dz = this.GetSignedDistance(pLocal + new Vector3(0, 0, e))
                - this.GetSignedDistance(pLocal - new Vector3(0, 0, e));

            return new Vector3(dx, dy, dz).normalized;
        }
    }

    // Unions two collision shapes
    public class UnionShape : BooleanShape
    {
        public CollisionShape a;
        public CollisionShape b;

        public UnionShape(CollisionShape a, CollisionShape b, Vector3 position, Vector3 rotation) : base(position, rotation)
        {
            this.position = position;
            this.rotation = Quaternion.Euler(rotation);
            this.a = a;
            this.b = b;
        }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            CollisionInfo aColl = this.a.GetCollisionInfo(pLocal);
            CollisionInfo bColl = this.b.GetCollisionInfo(pLocal);

            float aDist = aColl.signedDistance;
            float bDist = bColl.signedDistance;

            if (Mathf.Abs(aDist - bDist) > Constants.SEAM_EPS) return aDist < bDist ? aColl : bColl;

            CollisionInfo collisionInfo = new(
                Mathf.Min(aDist, bDist),
                this.EstimateNormal(pLocal),
                aColl.owner);

            return collisionInfo;
        }

        public override float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);

            float aDist = this.a.GetSignedDistance(pLocal);
            float bDist = this.b.GetSignedDistance(pLocal);
            return Mathf.Min(aDist, bDist);
        }
    }

    // Intersects two collision shapes
    public class IntersectShape : BooleanShape
    {
        public CollisionShape a;
        public CollisionShape b;

        public IntersectShape(CollisionShape a, CollisionShape b, Vector3 position, Vector3 rotation) : base(position, rotation)
        {
            this.a = a;
            this.b = b;
        }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            CollisionInfo aColl = this.a.GetCollisionInfo(pLocal);
            CollisionInfo bColl = this.b.GetCollisionInfo(pLocal);

            float aDist = aColl.signedDistance;
            float bDist = bColl.signedDistance;

            if (Mathf.Abs(aDist - bDist) > Constants.SEAM_EPS) return aDist > bDist ? aColl : bColl;

            CollisionInfo collisionInfo = new(
                Mathf.Max(aDist, bDist),
                this.EstimateNormal(pLocal),
                aColl.owner);

            return collisionInfo;
        }

        public override float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);

            float aDist = this.a.GetSignedDistance(pLocal);
            float bDist = this.b.GetSignedDistance(pLocal);
            return Mathf.Max(aDist, bDist);
        }
    }

    // Subtracts shape b from a: a - b
    public class DifferenceShape : BooleanShape
    {
        public CollisionShape a;
        public CollisionShape b;

        public DifferenceShape(CollisionShape a, CollisionShape b, Vector3 position, Vector3 rotation) : base(position, rotation)
        {
            this.a = a;
            this.b = b;
        }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            CollisionInfo aColl = this.a.GetCollisionInfo(pLocal);
            CollisionInfo bColl = this.b.GetCollisionInfo(pLocal);
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

        public override float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);

            float aDist = this.a.GetSignedDistance(pLocal);
            float bDist = this.b.GetSignedDistance(pLocal);
            return Mathf.Max(aDist, -bDist);
        }
    }

    // Inverses a collision shape
    public class InverseShape : BooleanShape
    {
        public CollisionShape shape;

        public InverseShape(CollisionShape shape, Vector3 position, Vector3 rotation) : base(position, rotation)
        {
            this.shape = shape;
        }

        protected override CollisionInfo GetCollisionInfoLocal(Vector3 pLocal)
        {
            CollisionInfo collisionInfo = shape.GetCollisionInfo(pLocal);
            collisionInfo.signedDistance *= -1;
            collisionInfo.collNormal *= -1;
            return collisionInfo;
        }

        public override float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);
            return -shape.GetSignedDistance(pLocal);
        }
    }
}
