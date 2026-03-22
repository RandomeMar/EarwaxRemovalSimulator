using UnityEngine;


namespace EarwaxSim
{
    // Object with a defined shape and material properties like friction. Stored inside collision constraint.
    public class CollisionObject
    {
        public ICollisionShape shape;
        public float dynamicFriction;

        public CollisionObject(ICollisionShape shape, float dynamicFriction)
        {
            this.shape = shape;
            this.dynamicFriction = dynamicFriction;
        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            return this.shape.GetCollisionInfo(particlePos);
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

        public void DrawSlice(ICollisionShape shape)
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
    public interface ICollisionShape
    {
        public (float, Vector3) GetCollisionInfo(Vector3 particlePos);
        public float GetSignedDistance(Vector3 particlePos);
    }

    // Defines plane collision shape
    public class PlaneShape : ICollisionShape
    {
        public Vector3 position;
        public Vector3 normal;
        public Quaternion rotation;


        public PlaneShape(Vector3 p0, Vector3 rotation)
        {
            this.position = p0;
            this.normal = Vector3.up;
            this.rotation = Quaternion.Euler(rotation);
        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);

            return (Vector3.Dot(this.normal, pLocal), this.rotation * this.normal);
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);
            return Vector3.Dot(this.normal, pLocal);
        }
    }

    // Defines sphere collision shape
    public class SphereShape : ICollisionShape
    {
        public Vector3 position;
        public Quaternion rotation;
        public float radius;

        public SphereShape(Vector3 position, Vector3 rotation, float radius)
        {
            this.position = position;
            this.rotation = Quaternion.Euler(rotation);
            this.radius = radius;
        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);

            Vector3 distVec = pLocal - Vector3.zero;

            if (distVec == Vector3.zero) return (-this.radius, Vector3.up); // Fallback in case particle is at sphere center

            return (distVec.magnitude - this.radius, this.rotation * distVec.normalized);
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);

            Vector3 distVec = pLocal - Vector3.zero;
            return distVec.magnitude - radius;
        }
    }

    // Defines capsule collision shape NOTE: Rotation and position have not been implemented yet
    public class CapsuleShape : ICollisionShape
    {
        public Vector3 a;
        public Vector3 b;
        public float radius;

        // Cached values
        private Vector3 ba;
        private float baSqrMag;

        public CapsuleShape(Vector3 a, Vector3 b, float radius)
        {
            this.a = a;
            this.b = b;
            this.ba = b - a;
            this.baSqrMag = Vector3.Dot(this.ba, this.ba);
            this.radius = radius;
        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            float t = Vector3.Dot((particlePos - this.a), ba) / this.baSqrMag;
            t = Mathf.Clamp(t, 0f, 1f); // 0 = a, 1 = b

            Vector3 q = this.a + t * this.ba; // Position of closest point on line segment a to b

            Vector3 normal = (particlePos - q);
            float signedDistance = normal.magnitude - this.radius;

            return (signedDistance, normal.normalized);
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            float t = Vector3.Dot((particlePos - this.a), ba) / this.baSqrMag;
            t = Mathf.Clamp(t, 0f, 1f); // 0 = a, 1 = b

            Vector3 q = this.a + t * this.ba; // Position of closest point on line segment a to b

            Vector3 normal = (particlePos - q);
            return normal.magnitude - this.radius;
        }
    }

    // Defines box collision shape
    public class BoxShape : ICollisionShape
    {
        public Vector3 center;
        public Quaternion rotation;
        public Vector3 b; // half-extents


        public BoxShape(Vector3 center, Vector3 rotation, Vector3 b)
        {
            this.center = center;
            this.rotation = Quaternion.Euler(rotation);
            this.b = b;
        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.center);

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

            return (signedDistance, this.rotation * collNormal);
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.center);

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


    // ------ Boolean Operations ------

    // Unions two collision shapes
    public class UnionShape : ICollisionShape
    {
        public Vector3 position;
        public Quaternion rotation;
        public ICollisionShape a;
        public ICollisionShape b;

        public UnionShape(ICollisionShape a, ICollisionShape b, Vector3 position, Vector3 rotation)
        {
            this.position = position;
            this.rotation = Quaternion.Euler(rotation);
            this.a = a;
            this.b = b;
        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);
            float signedDistance;
            Vector3 collNormal;

            (float aDist, Vector3 aNorm) = this.a.GetCollisionInfo(pLocal);
            (float bDist, Vector3 bNorm) = this.b.GetCollisionInfo(pLocal);
            signedDistance = Mathf.Min(aDist, bDist);

            if (Mathf.Abs(aDist - bDist) > Constants.SEAM_EPS)
            {
                collNormal = aDist < bDist ? aNorm : bNorm;
            }
            else collNormal = this.EstimateNormal(pLocal);

            collNormal = this.rotation * collNormal;
            return (signedDistance, collNormal);
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);

            float aDist = this.a.GetSignedDistance(pLocal);
            float bDist = this.b.GetSignedDistance(pLocal);
            return Mathf.Min(aDist, bDist);
        }

        private Vector3 EstimateNormal(Vector3 particlePos)
        {
            float e = .001f;

            // This estimates the gradient of the union at positon particlePos
            float dx = this.GetSignedDistance(particlePos + new Vector3(e, 0, 0))
                - this.GetSignedDistance(particlePos - new Vector3(e, 0, 0));

            float dy = this.GetSignedDistance(particlePos + new Vector3(0, e, 0))
                - this.GetSignedDistance(particlePos - new Vector3(0, e, 0));

            float dz = this.GetSignedDistance(particlePos + new Vector3(0, 0, e))
                - this.GetSignedDistance(particlePos - new Vector3(0, 0, e));

            return new Vector3(dx, dy, dz).normalized;
        }
    }

    // Intersects two collision shapes
    public class IntersectShape : ICollisionShape
    {
        public Vector3 position;
        public Quaternion rotation;
        public ICollisionShape a;
        public ICollisionShape b;

        public IntersectShape(ICollisionShape a, ICollisionShape b, Vector3 position, Vector3 rotation)
        {
            this.position = position;
            this.rotation = Quaternion.Euler(rotation);
            this.a = a;
            this.b = b;
        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);
            float signedDistance;
            Vector3 collNormal;

            (float aDist, Vector3 aNorm) = this.a.GetCollisionInfo(pLocal);
            (float bDist, Vector3 bNorm) = this.b.GetCollisionInfo(pLocal);
            signedDistance = Mathf.Max(aDist, bDist);

            if (Mathf.Abs(aDist - bDist) > Constants.SEAM_EPS)
            {
                collNormal = aDist > bDist ? aNorm : bNorm;
            }
            else collNormal = this.EstimateNormal(pLocal);

            collNormal = this.rotation * collNormal;
            return (signedDistance, collNormal);
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);

            float aDist = this.a.GetSignedDistance(pLocal);
            float bDist = this.b.GetSignedDistance(pLocal);
            return Mathf.Max(aDist, bDist);
        }

        private Vector3 EstimateNormal(Vector3 particlePos)
        {
            float e = .001f;

            // This estimates the gradient of the intersection at positon particlePos
            float dx = this.GetSignedDistance(particlePos + new Vector3(e, 0, 0))
                - this.GetSignedDistance(particlePos - new Vector3(e, 0, 0));

            float dy = this.GetSignedDistance(particlePos + new Vector3(0, e, 0))
                - this.GetSignedDistance(particlePos - new Vector3(0, e, 0));

            float dz = this.GetSignedDistance(particlePos + new Vector3(0, 0, e))
                - this.GetSignedDistance(particlePos - new Vector3(0, 0, e));

            return new Vector3(dx, dy, dz).normalized;
        }
    }

    // Subtracts shape b from a: a - b
    public class DifferenceShape : ICollisionShape
    {
        public Vector3 position;
        public Quaternion rotation;
        public ICollisionShape a;
        public ICollisionShape b;

        public DifferenceShape(ICollisionShape a, ICollisionShape b, Vector3 position, Vector3 rotation)
        {
            this.position = position;
            this.rotation = Quaternion.Euler(rotation);
            this.a = a;
            this.b = b;
        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);
            float signedDistance;
            Vector3 collNormal;

            (float aDist, Vector3 aNorm) = this.a.GetCollisionInfo(pLocal);
            (float bDist, Vector3 bNorm) = this.b.GetCollisionInfo(pLocal);
            bDist *= -1f;
            bNorm *= -1f;

            signedDistance = Mathf.Max(aDist, bDist);

            if (Mathf.Abs(aDist - bDist) > Constants.SEAM_EPS)
            {
                collNormal = aDist > bDist ? aNorm : bNorm;
            }
            else collNormal = this.EstimateNormal(pLocal);

            collNormal = this.rotation * collNormal;
            return (signedDistance, collNormal);
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);

            float aDist = this.a.GetSignedDistance(pLocal);
            float bDist = this.b.GetSignedDistance(pLocal);
            return Mathf.Max(aDist, -bDist);
        }

        private Vector3 EstimateNormal(Vector3 particlePos)
        {
            float e = .001f;

            // This estimates the gradient of the difference at positon particlePos
            float dx = this.GetSignedDistance(particlePos + new Vector3(e, 0, 0))
                - this.GetSignedDistance(particlePos - new Vector3(e, 0, 0));

            float dy = this.GetSignedDistance(particlePos + new Vector3(0, e, 0))
                - this.GetSignedDistance(particlePos - new Vector3(0, e, 0));

            float dz = this.GetSignedDistance(particlePos + new Vector3(0, 0, e))
                - this.GetSignedDistance(particlePos - new Vector3(0, 0, e));

            return new Vector3(dx, dy, dz).normalized;
        }
    }

    // Inverses a collision shape
    public class InverseShape : ICollisionShape
    {
        public Vector3 position;
        public Quaternion rotation;
        public ICollisionShape shape;

        public InverseShape(ICollisionShape shape, Vector3 position, Vector3 rotation)
        {
            this.position = position;
            this.rotation = Quaternion.Euler(rotation);
            this.shape = shape;
        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);

            (float signedDistance, Vector3 normal) = shape.GetCollisionInfo(pLocal);
            return (-signedDistance, -(this.rotation * normal));
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = Quaternion.Inverse(this.rotation) * (particlePos - this.position);
            return -shape.GetSignedDistance(pLocal);
        }
    }
}
