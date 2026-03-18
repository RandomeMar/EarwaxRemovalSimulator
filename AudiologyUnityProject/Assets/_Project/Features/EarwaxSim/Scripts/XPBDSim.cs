using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Owns the full XPBD loop
public class XPBDSim : MonoBehaviour
{
    #region Public Parameters
    [Header("Gizmo Debug Settings")]
    public bool drawParticles = true;
    public bool drawDist = true;

    [Header("Solver Settings")]
    [Min(1f)]
    public int solverIterations;
    public Vector3 gravity = new Vector3(0, -9.8f, 0);
    [Range(0, 1)]
    public float globalDamping = 1f; // 0 to 1

    [Header("Lattice Settings")]
    public Vector3 latticeOrigin = Vector3.zero;
    [Min(2f)]
    public int latticeParticleCount = 3;
    [Min(.01f)]
    public float latticeLength = 2.0f;

    [Header("Material Settings")]
    [Min(0f)]
    public float materialDensity;
    [Min(0f)]
    public float baseBondCompliance;

    [Header("Friction Settings")]
    [Min(0f)]
    public float dynamicFriction;

    [Header("Distance Constraint Settings")]
    public bool distOn = true;
    public float yieldStrainMult = 2f;
    public float plasticFlow = 1f;
    public float breakStrainMult = 4f;
    
    [Header("Visco-elasticity Settings")]
    [Min(0f)]
    public float adaptRate;
    [Min(0f)]
    public float recoveryRate;

    [Header("Density Constraint Settings")]
    public bool denseOn = true;
    [Min(0f)]
    public float denseCompliance;
    [Min(1f)]
    public float hMult = 1.25f;

    [Header("Collision Constraint Settings")]
    public bool collOn = true;
    [Min(0f)]
    public float collCompliance;
    public Vector3 roomDimensions = new Vector3(4f, 4f, 4f);
    public Vector3 roomRotation = Vector3.zero;

    [Header("Tool Settings")]
    public Vector3 toolSpawn = Vector3.zero;
    [Min(0f)]
    public float toolRadius = .5f;
    [Min(0f)]
    public float toolSpeed = 3f;
    #endregion

    #region Constants
    const float EPS = 1e-6f;
    const float SEAM_EPS = 1e-6f;

    const int MAX_NEIGHBORS = 64;
    const int MAX_PARTICLES = 1000;
    #endregion

    #region Private Input Values
    private PlayerInput playerInput;
    private InputAction moveToolAction;
    private Vector3 moveDir;

    private BoxShape roomArea;
    private SphereShape toolShape;
    #endregion

    #region Solver Objects
    ParticleSet ps;
    SpatialHash grid;
    DistanceConstraintSet dist;
    DensityConstraintSolver dense;
    CollisionConstraintSolver coll;
    #endregion


    // Classes/Structs:

    // Contains particle positions, velocities, and mass values
    class ParticleSet
    {
        public Vector3[] currentPosition;
        public Vector3[] previousPosition;
        public Vector3[] velocity;
        public float[] invMass;
        public float[] mass;

        public int count;

        public ParticleSet(int count)
        {
            this.count = count;
            this.currentPosition = new Vector3[count];
            this.previousPosition = new Vector3[count];
            this.velocity = new Vector3[count];
            this.invMass = new float[count];
            this.mass = new float[count];
        }
    }

    // Dict used to store particles in a 3D grid
    class SpatialHash
    {
        float cellSize;
        Dictionary<long, int> bucketHeads;
        int[] next;
        int[] neighborBuffer;

        public SpatialHash(float cellSize, int particleCount)
        {
            this.cellSize = cellSize;
            this.bucketHeads = new(particleCount);
            this.next = new int[particleCount];
            this.neighborBuffer = new int[MAX_NEIGHBORS]; // Array of neighbor ints to be reused for GetNeighbors
        }

        public (int, int, int) CalcCellCoord(Vector3 position)
            {
                return (
                    Mathf.FloorToInt(position.x / cellSize),
                    Mathf.FloorToInt(position.y / cellSize),
                    Mathf.FloorToInt(position.z / cellSize)
                    );
            }

        public long HashCoord(int x_coord, int y_coord, int z_coord)
        {
            const int SHIFT = 20;

            long x = (long)(x_coord + (1 << SHIFT));
            long y = (long)(y_coord + (1 << SHIFT));
            long z = (long)(z_coord + (1 << SHIFT));

            return (x << 42) | (y << 21) | z;
        }

        // Rebuilds grid housing particles
        public void BuildGrid(ParticleSet ps)
        {
            bucketHeads.Clear();

            for (int i = 0; i < ps.count; i++)
            {
                (int x, int y, int z) = CalcCellCoord(ps.currentPosition[i]);
                long key = HashCoord(x, y, z);
                if (!this.bucketHeads.TryGetValue(key, out int oldHead))
                {
                    this.bucketHeads[key] = i;
                    this.next[i] = -1;
                }
                else
                {
                    this.next[i] = oldHead;
                    this.bucketHeads[key] = i;
                }
            }
        }

        // Returns the indexes of neighbors to particle i along with the amount of neighbors
        public (int[], int) GetNeighbors(ParticleSet ps, int i, float h)
        {
            (int base_x, int base_y, int base_z) = CalcCellCoord(ps.currentPosition[i]);
            int neighborCount = 0;
            Vector3 distVec = Vector3.zero;
            float r2 = 0f;
            float h2 = h * h;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        // If too many neighbors for buffer
                        if (neighborCount >= this.neighborBuffer.Length)
                            return (this.neighborBuffer, neighborCount);

                        long key = HashCoord(base_x + dx, base_y + dy, base_z + dz);

                        // If no particles in cell
                        if (!this.bucketHeads.TryGetValue(key, out int j))
                        {
                            continue;
                        }

                        // For first index in bucket
                        if (j != i)
                        {
                            distVec = ps.currentPosition[i] - ps.currentPosition[j];
                            r2 = distVec.sqrMagnitude;
                            if (r2 <= h2)
                            {
                                this.neighborBuffer[neighborCount] = j;
                                neighborCount++;
                            }
                        }

                        while (next[j] != -1)
                        {
                            // If too many neighbors for buffer
                            if (neighborCount >= this.neighborBuffer.Length)
                                return (this.neighborBuffer, neighborCount);

                            j = next[j];

                            if (j != i)
                            {
                                distVec = ps.currentPosition[i] - ps.currentPosition[j];
                                r2 = distVec.sqrMagnitude;
                                if (r2 <= h2)
                                {
                                    this.neighborBuffer[neighborCount] = j;
                                    neighborCount++;
                                }
                            }
                        }
                    }
            return (this.neighborBuffer, neighborCount);
        }

    }

    #region Collision Shapes

    // Collision shapes only define shape
    interface ICollisionShape
    {
        public (float, Vector3) GetCollisionInfo(Vector3 particlePos);
        public float GetSignedDistance(Vector3 particlePos);
    }

    // Collision object has shape and material properties like friction
    class CollisionObject
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


    // Defines plane collision shape
    class PlaneShape : ICollisionShape
    {
        public Vector3 p0;
        public Vector3 normal;

        public PlaneShape(Vector3 p0, Vector3 planeNormal)
        {
            this.p0 = p0;
            this.normal = planeNormal.normalized;
        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            return (Vector3.Dot(this.normal, (particlePos - this.p0)), this.normal);
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            return Vector3.Dot(this.normal, (particlePos - this.p0));
        }
    }

    // Defines sphere collision shape
    class SphereShape : ICollisionShape
    {
        public Vector3 center;
        public float radius;

        public SphereShape(Vector3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            Vector3 distVec = particlePos - this.center;
            return (distVec.magnitude - radius, distVec.normalized);
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 distVec = particlePos - this.center;
            return distVec.magnitude - radius;
        }
    }

    // Defines capsule collision shape
    class CapsuleShape : ICollisionShape
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
    class BoxShape : ICollisionShape
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
            if (outside.sqrMagnitude > EPS)
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


    // Unions two collision shapes UNFINISHED
    class UnionShape : ICollisionShape
    {
        public ICollisionShape a;
        public ICollisionShape b;

        public UnionShape(ICollisionShape a, ICollisionShape b)
        {
            this.a = a;
            this.b = b;
        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            (float aDist, Vector3 aNorm) = this.a.GetCollisionInfo(particlePos);
            (float bDist, Vector3 bNorm) = this.b.GetCollisionInfo(particlePos);

            if (Mathf.Abs(aDist - bDist) > SEAM_EPS)
            {
                if (aDist < bDist) return (aDist, aNorm);
                else return (bDist, bNorm);
            }
            else
            {
                float d = Mathf.Min(aDist, bDist);
                return (d, this.EstimateNormal(particlePos));
            }
        }

        private Vector3 EstimateNormal(Vector3 particlePos)
        {
            float CombinedDistance(Vector3 p)
            {
                float d1 = this.a.GetSignedDistance(p);
                float d2 = this.b.GetSignedDistance(p);
                return Mathf.Min(d1, d2); // union
            }

            float e = .001f;

            // This estimates the gradient of the union at positon particlePos
            float dx = CombinedDistance(particlePos + new Vector3(e, 0, 0))
                - CombinedDistance(particlePos - new Vector3(e, 0, 0));

            float dy = CombinedDistance(particlePos + new Vector3(0, e, 0))
                - CombinedDistance(particlePos - new Vector3(0, e, 0));

            float dz = CombinedDistance(particlePos + new Vector3(0, 0, e))
                - CombinedDistance(particlePos - new Vector3(0, 0, e));

            return new Vector3(dx, dy, dz).normalized;
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            float aDist = this.a.GetSignedDistance(particlePos);
            float bDist = this.b.GetSignedDistance(particlePos);
            return Mathf.Min(aDist, bDist);
        }
    }

    // Intersects two collision shapes UNFINISHED
    class IntersectShape : ICollisionShape
    {
        public IntersectShape()
        {

        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            return (0f, Vector3.zero);
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            return 0f;
        }
    }

    // Differences two collision shapes UNFINISHED
    class DifferenceShape : ICollisionShape
    {
        public DifferenceShape()
        {

        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            return (0f, Vector3.zero);
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            return 0f;
        }
    }

    // Inverses a collision shape
    class InverseShape : ICollisionShape
    {
        public ICollisionShape shape;

        public InverseShape(ICollisionShape shape)
        {
            this.shape = shape;
        }

        public (float, Vector3) GetCollisionInfo(Vector3 particlePos)
        {
            (float signedDistance, Vector3 normal) = shape.GetCollisionInfo(particlePos);
            return (-signedDistance, -normal);
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            return -shape.GetSignedDistance(particlePos);
        }
    }
    #endregion

    #region Constraint Solvers

    // Interface for all constraint solvers
    interface IConstraintSolver
    {
        void ResetLambda();
        void SolveOnce(ParticleSet ps, float dt, SpatialHash grid);
    }

    // Single distance constraint between two particles
    struct DistanceConstraint
    {
        public int i, j;
        public float origRestLength;
        public float restLength;
        public float compliance;
        public float lambda;
        public bool active;

        public DistanceConstraint(int i, int j, float restLength, float compliance)
        {
            this.i = i;
            this.j = j;
            this.origRestLength = restLength;
            this.restLength = restLength;
            this.compliance = compliance;
            this.lambda = 0f;

            this.active = true;
        }
    }

    // Solves distance constraints
    class DistanceConstraintSet : IConstraintSolver
    {
        public DistanceConstraint[] constraints;

        // For plastic deformation
        public float yieldStrain;
        public float plasticFlow;
        public float breakStrain;

        // For visco-elasticity
        public float adaptRate;
        public float recRate;

        public DistanceConstraintSet(DistanceConstraint[] constraints, float yieldStrain, float plasticFlow, float breakStrain, float adaptRate, float recRate)
        {
            this.constraints = constraints;
            this.yieldStrain = yieldStrain;
            this.plasticFlow = plasticFlow;
            this.breakStrain = breakStrain;

            this.adaptRate = adaptRate;
            this.recRate = recRate;
        }

        public void ResetLambda()
        {
            for (int i = 0; i < this.constraints.Length; i++)
            {
                this.constraints[i].lambda = 0;
            }
        }

        public void SolveOnce(ParticleSet ps, float dt, SpatialHash grid)
        {
            for (int index = 0; index < this.constraints.Length; index++)
            {
                DistanceConstraint constraint = this.constraints[index];

                if (!constraint.active) continue;

                int i = constraint.i;
                int j = constraint.j;
                Vector3 d = ps.currentPosition[i] - ps.currentPosition[j]; // Vector from j to i
                float l = d.magnitude; // Distance between i and j
                float c = l - constraint.restLength; // C

                // if l is close to 0: continue
                if (l <= EPS) { continue; }

                Vector3 n = d / l; // Normalized direction from j to i

                float alpha = constraint.compliance / (dt * dt);

                // If the denominator is close to 0: continue
                float denom = (ps.invMass[i] + ps.invMass[j] + alpha);
                if (denom <= EPS) { continue; }

                float deltaLambda = (-c - alpha * constraint.lambda) / denom;

                constraint.lambda += deltaLambda;
                this.constraints[index] = constraint;

                ps.currentPosition[i] += ps.invMass[i] * n * deltaLambda;
                ps.currentPosition[j] -= ps.invMass[j] * n * deltaLambda;
            }
        }

        public void UpdateRestLengths(ParticleSet ps, float dt)
        {
            for (int index = 0; index < this.constraints.Length; index++)
            {
                DistanceConstraint constraint = this.constraints[index];

                if (!constraint.active) continue;

                float currLength = Vector3.Distance(ps.currentPosition[constraint.i], ps.currentPosition[constraint.j]);

                // Makes rest length closer to the current length of the constraint
                constraint.restLength += this.adaptRate * (currLength - constraint.restLength) * dt;

                // Makes rest length closer to the original rest length of the constraint
                constraint.restLength += this.recRate * (constraint.origRestLength - constraint.restLength) * dt;

                this.constraints[index].restLength = constraint.restLength;


                // ------ Plastic Deformation ------
                float strain = (currLength - constraint.origRestLength) / constraint.origRestLength;

                if (strain >= breakStrain)
                {
                    this.constraints[index].active = false;
                }

                if (Mathf.Abs(strain) <= this.yieldStrain) continue;

                float deltaRestLen = this.plasticFlow * (Mathf.Abs(strain) - this.yieldStrain) * Mathf.Sign(strain) * constraint.origRestLength;

                this.constraints[index].origRestLength += deltaRestLen;
            }
        }
    }

    // Prevents particles from getting too close together
    class DensityConstraintSolver : IConstraintSolver
    {
        public float restDensity;
        public float h;
        public float compliance;

        private float[] lambda;
        private Vector3[] deltaX; // Used for Jacobi structure
        private Vector3[] gradBuffer;

        // Cached values for better performance
        private float h2;
        private float poly6Coef;
        private float poly6GradCoef;

        public DensityConstraintSolver(float restDensity, float h, float compliance)
        {
            this.restDensity = restDensity;
            this.h = h;
            this.h2 = h * h;
            this.compliance = compliance;
            this.poly6Coef = (float)(315f / (64f * Mathf.PI * Mathf.Pow(h, 9)));
            this.poly6GradCoef = (float)(945f / (32f * Mathf.PI * Mathf.Pow(h, 9)));
        }

        private void EnsureCapacity(int count)
        {
            if (this.deltaX == null || this.deltaX.Length != count)
                this.deltaX = new Vector3[count];

            if (this.lambda == null || this.lambda.Length != count)
                this.lambda = new float[count];

            if (this.gradBuffer == null || this.gradBuffer.Length < MAX_NEIGHBORS)
                this.gradBuffer = new Vector3[MAX_NEIGHBORS];
        }

        public void ResetLambda()
        {
            if (this.lambda == null) return;
            Array.Clear(this.lambda, 0, this.lambda.Length);
        }

        public void SolveOnce(ParticleSet ps, float dt, SpatialHash grid)
        {
            EnsureCapacity(ps.count);
            Array.Clear(this.deltaX, 0, ps.count);

            float alpha = this.compliance / (dt * dt);

            for (int i = 0; i < ps.count; i++)
            {
                if (ps.invMass[i] <= 0) continue;

                int[] js;
                int jCount;

                (js, jCount) = grid.GetNeighbors(ps, i, this.h);

                float density = 0f;
                Vector3 gradi = Vector3.zero;
                float denom = 0f;

                for (int iter = 0; iter < jCount; iter++)
                {
                    int j = js[iter];

                    if (ps.invMass[j] <= 0) continue;
                    float mj = ps.mass[j];

                    Vector3 distVec = ps.currentPosition[i] - ps.currentPosition[j];
                    float term = (this.h2 - distVec.sqrMagnitude);

                    density += mj * this.poly6Coef * term * term * term; // Poly-6 kernel smoothing function
                    this.gradBuffer[iter] = mj * this.poly6GradCoef * term * term * distVec; // Gradient of Poly-6

                    gradi -= this.gradBuffer[iter];
                    denom += this.gradBuffer[iter].sqrMagnitude * ps.invMass[j];
                }

                float c = Mathf.Max(density / this.restDensity - 1f, 0f); // This makes it so density only pushes, no pulling
                denom += gradi.sqrMagnitude * ps.invMass[i];

                // If the denominator is close to 0: continue
                if (denom + alpha <= EPS) continue;

                float deltaLambda = (-c - alpha * this.lambda[i]) / (denom + alpha);
                this.lambda[i] += deltaLambda;

                this.deltaX[i] += ps.invMass[i] * gradi * deltaLambda;

                for (int iter = 0; iter < jCount; iter++)
                {
                    int j = js[iter];

                    if (ps.invMass[j] <= 0) continue;

                    this.deltaX[j] += ps.invMass[j] * this.gradBuffer[iter] * deltaLambda;
                }

            }

            // Jacobi structure
            for (int i = 0; i < ps.count; i++)
            {
                ps.currentPosition[i] += deltaX[i];
            }

        }
    }

    // Solves collisions between particles and collision shapes
    class CollisionConstraintSolver : IConstraintSolver
    {
        public List<CollisionObject> objects;
        public float compliance;

        public CollisionConstraintSolver(float compliance)
        {
            this.objects = new(1);
            this.compliance = compliance;
        }

        public void ResetLambda()
        {

        }

        public void SolveOnce(ParticleSet ps, float dt, SpatialHash grid)
        {
            // NOTE: Currently this does not have lambda. Eventually, lambda may be implemented.
            float alpha = this.compliance / (dt * dt);

            for (int i = 0; i < ps.count; i++)
            {
                if (ps.invMass[i] == 0) continue;

                foreach (CollisionObject obj in this.objects)
                {
                    // Calculate C and gradient of C
                    (float c, Vector3 collisionNormal) = obj.GetCollisionInfo(ps.currentPosition[i]);
                    // As long as C is not negative, we assume there is no correction to be made
                    if (c >= 0) continue;

                    // Check if denom is close to 0
                    float denom = (ps.invMass[i] + alpha);
                    if (denom <= EPS) continue;

                    // Calculate delta lambda
                    float deltaLambda = -c / denom;

                    // Calculate normal correction
                    Vector3 normalCorrection = ps.invMass[i] * collisionNormal * deltaLambda;

                    // Update position
                    ps.currentPosition[i] += normalCorrection;


                    // ------ Friction ------
                    Vector3 dx = ps.currentPosition[i] - ps.previousPosition[i];
                    Vector3 dxNormal = Vector3.Dot(dx, collisionNormal) * collisionNormal;
                    Vector3 dxTangent = dx - dxNormal;

                    float tangLength = dxTangent.magnitude;

                    if (tangLength > EPS)
                    {
                        float maxFriction = obj.dynamicFriction * normalCorrection.magnitude;

                        // Friction cannot cause the particle to move backwards, only resist forward motion
                        Vector3 frictionCorrection = -dxTangent.normalized * Mathf.Min(maxFriction, tangLength);

                        // Update position
                        ps.currentPosition[i] += frictionCorrection;
                    }
                }
            }
        }
    }
    #endregion


    // Functions

    // Smoothing kernel for calculating density DEPRECIATED
    static float Poly6(float r2, float h)
    {
        float h2 = h * h;
        if (h2 < r2) return 0;

        float h4 = h2 * h2;

        float term = h2 - r2;

        return 315 / (64 * Mathf.PI * h4 * h4 * h) * term * term * term;
    }

    int CalcIndex(int i, int j, int k, int n)
    {
        return n * (n * i + j) + k;
    }

    float CalcRestDensity(ParticleSet ps, SpatialHash grid, float h)
    {
        int n = latticeParticleCount;
        int i = CalcIndex(n/2, n/2, n/2, n);
        float density = 0f;

        int[] js;
        int jCount;
        (js, jCount) = grid.GetNeighbors(ps, i, h);

        for (int iter = 0; iter < jCount; iter++)
        {
            int j = js[iter];

            if (ps.invMass[j] <= 0) continue;
            float mj = 1f / ps.invMass[j];

            Vector3 distVec = ps.currentPosition[i] - ps.currentPosition[j];

            density += mj * Poly6(distVec.sqrMagnitude, h);
        }

        return density;
    }

    (ParticleSet, SpatialHash, DistanceConstraintSet, DensityConstraintSolver) GenerateLattice()
    {
        int n = latticeParticleCount;
        int particleCount = n * n * n;
        float length = latticeLength;
        float spacing = length / (n - 1);
        float h = spacing * hMult;

        float totalVolume = length * length * length;
        float totalMass = materialDensity * totalVolume;
        float particleMass = totalMass / particleCount;
        float invMass = 1f / particleMass;
        float bondCompliance = baseBondCompliance * spacing;
        
        ParticleSet lattice = new(particleCount);
        List<DistanceConstraint> dist = new();

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                for(int k = 0; k < n; k++)
                {
                    // Particles
                    int curr = CalcIndex(i, j, k, n);
                    lattice.currentPosition[curr] = new(-length / 2 + i * spacing, -length / 2 + j * spacing, -length / 2 + k * spacing);
                    lattice.currentPosition[curr] += latticeOrigin;


                    lattice.previousPosition[curr] = lattice.currentPosition[curr];
                    lattice.velocity[curr] = new(0, 0, 0);
                    lattice.invMass[curr] = invMass;
                    lattice.mass[curr] = particleMass;

                    // Constraints
                    if (i + 1 < n)
                    {
                        int iPlus = CalcIndex(i + 1, j, k, n);
                        dist.Add(new DistanceConstraint(curr, iPlus, spacing, bondCompliance));

                        if (j + 1 < n)
                        {
                            int jPlus = CalcIndex(i, j + 1, k, n);
                            dist.Add(new DistanceConstraint(iPlus, jPlus, Mathf.Sqrt(2) * spacing, bondCompliance));
                            int ijPlus = CalcIndex(i + 1, j + 1, k, n);
                            dist.Add(new DistanceConstraint(curr, ijPlus, Mathf.Sqrt(2) * spacing, bondCompliance));
                        }

                    }
                    if (j + 1 < n)
                    {
                        int jPlus = CalcIndex(i, j + 1, k, n);
                        dist.Add(new DistanceConstraint(curr, jPlus, spacing, bondCompliance));

                        if (k + 1 < n)
                        {
                            int kPlus = CalcIndex(i, j, k + 1, n);
                            dist.Add(new DistanceConstraint(jPlus, kPlus, Mathf.Sqrt(2) * spacing, bondCompliance));
                            int jkPlus = CalcIndex(i, j + 1, k + 1, n);
                            dist.Add(new DistanceConstraint(curr, jkPlus, Mathf.Sqrt(2) * spacing, bondCompliance));
                        }
                    }
                    if (k + 1 < n)
                    {
                        int kPlus = CalcIndex(i, j, k + 1, n);
                        dist.Add(new DistanceConstraint(curr, kPlus, spacing, bondCompliance));

                        if (i + 1 < n)
                        {
                            int iPlus = CalcIndex(i + 1, j, k, n);
                            dist.Add(new DistanceConstraint(kPlus, iPlus, Mathf.Sqrt(2) * spacing, bondCompliance));
                            int kiPlus = CalcIndex(i + 1, j, k + 1, n);
                            dist.Add(new DistanceConstraint(curr, kiPlus, Mathf.Sqrt(2) * spacing, bondCompliance));
                        }
                    }
                }
            }
        }

        DistanceConstraintSet dcs = new(dist.ToArray(), spacing * yieldStrainMult, plasticFlow, spacing * breakStrainMult, adaptRate, recoveryRate);

        SpatialHash grid = new SpatialHash(h, particleCount);

        grid.BuildGrid(lattice);

        float restDensity = CalcRestDensity(lattice, grid, h);

        DensityConstraintSolver dense = new(restDensity, h, denseCompliance);

        print("GENERATED LATTICE!!!");

        return (lattice, grid, dcs, dense);
    }

    void CreateBasicRoom(CollisionConstraintSolver coll)
    {
        //roomArea = new(new Vector3(0f, roomDimensions.y, 0f), roomRotation, roomDimensions);
        //InverseShape room = new(roomArea);
        //CollisionObject roomObj = new(room, dynamicFriction);
        //coll.objects.Add(roomObj);

        SphereShape s1 = new(new Vector3(-roomDimensions.x / 2, roomDimensions.y, 0f), roomDimensions.y);
        SphereShape s2 = new(new Vector3(roomDimensions.x / 2, roomDimensions.y, 0f), roomDimensions.y);
        UnionShape union = new UnionShape(s1, s2);
        InverseShape room = new(union);
        CollisionObject roomObj = new(room, dynamicFriction);
        coll.objects.Add(roomObj);

    }

    void AddTool(CollisionConstraintSolver coll, Vector3 center, float radius)
    {
        toolShape = new(center, radius);
        CollisionObject toolObj = new(toolShape, dynamicFriction);
        coll.objects.Add(toolObj);
    }

    // Creates lattice, room, and sphere tool
    void BuildSimulation()
    {
        (ps, grid, dist, dense) = GenerateLattice();
        coll = new CollisionConstraintSolver(collCompliance);
        AddTool(coll, toolSpawn, toolRadius);
        CreateBasicRoom(coll); // Make sure the room is the last collision shape added to coll. The last added collision shape has priority over the others
    }

    void ApplyForces(ParticleSet ps, float dt)
    {
        for (int i = 0; i < ps.velocity.Length; i++)
        {
            if (ps.invMass[i] == 0) continue; // Particles with invMass 0 should not move
            ps.velocity[i] += gravity * dt;
        }
    }

    void PredictPositions(ParticleSet ps, float dt)
    {
        for (int i = 0; i < ps.velocity.Length; i++)
        {
            if (ps.invMass[i] == 0) continue; // Particles with invMass 0 should not move
            ps.previousPosition[i] = ps.currentPosition[i];
            ps.currentPosition[i] += ps.velocity[i] * dt;
        }
    }
    
    void UpdateVelocities(ParticleSet ps, float dt)
    {
        for (int i = 0; i < ps.velocity.Length; i++)
        {
            if (ps.invMass[i] == 0) continue; // Particles with invMass 0 should not move
            ps.velocity[i] = (ps.currentPosition[i] - ps.previousPosition[i]) / dt;

            ps.velocity[i] *= globalDamping; // Velocity damping
        }
    }

    #region Mouse Interaction Functions
    private int grabbedParticle = -1;
    Plane dragPlane;
    private int selectedParticle = -1;
    
    int SelectParticle()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        float closestDist = float.MaxValue;
        int closestIndex = -1;
        float radius = 0.1f; // same as Gizmo sphere size

        for (int i = 0; i < ps.count; i++)
        {
            Vector3 center = ps.currentPosition[i];

            // Ray-sphere intersection test
            Vector3 oc = ray.origin - center;
            float a = Vector3.Dot(ray.direction, ray.direction);
            float b = 2.0f * Vector3.Dot(oc, ray.direction);
            float c = Vector3.Dot(oc, oc) - radius * radius;
            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0) continue;

            float t = (-b - Mathf.Sqrt(discriminant)) / (2.0f * a);

            if (t > 0 && t < closestDist)
            {
                closestDist = t;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    void BeginDrag(int selectedIndex)
    {
        // Get drag plane
        Vector3 planeNormal = Camera.main.transform.forward;
        Vector3 planePoint = ps.currentPosition[selectedIndex];
        dragPlane = new(planeNormal, planePoint);
    }

    void DragUpdate(int selectedIndex)
    {
        if (selectedIndex == -1) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 target = ray.GetPoint(enter);

            // TODO: I may later change this to a constraint instead of manually altering particle values
            ps.currentPosition[selectedIndex] = target;
            ps.previousPosition[selectedIndex] = target;
            ps.velocity[selectedIndex] = Vector3.zero;
        }
    }
    #endregion


    // ------ Monobehaviour Methods ------

    // Rebuilds the sim anytime parameters are changed in the editor
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        BuildSimulation();
    }

    // Called when the script instance is first initialized
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveToolAction = playerInput.actions.FindAction("MoveTool");
    }

    // Called before the first frame
    private void Start()
    {
        print("START");
        BuildSimulation();
    }

    // User input loop
    private void Update()
    {
        // WASD input
        moveDir = moveToolAction.ReadValue<Vector3>();

        // Mouse manipulation
        if (Input.GetMouseButtonUp(0))
        {
            grabbedParticle = -1;
            return;
        }

        if (grabbedParticle != -1)
        {
            BeginDrag(grabbedParticle);
            DragUpdate(grabbedParticle);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            grabbedParticle = SelectParticle();
        }

        if (Input.GetMouseButtonDown(1))
        {
            selectedParticle = SelectParticle();
        }
    }

    // Sim physics Loop
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Move ball tool
        toolShape.center += moveDir * toolSpeed * dt;
        

        // 1. Reset lambda
        if (distOn) dist.ResetLambda();
        if (denseOn) dense.ResetLambda();

        // 2. Apply external forces
        ApplyForces(ps, dt);

        // 3. Predict positions
        PredictPositions(ps, dt);

        // 4. Build spatial grid NOTE: Unsure if grid should be built per frame or per iteration
        if (denseOn) grid.BuildGrid(ps);

        // 5. Solve constraints
        for (int i = 0; i < solverIterations; i++)
        {
            if (distOn) dist.SolveOnce(ps, dt, grid);
            if (denseOn) dense.SolveOnce(ps, dt, grid);
            if (collOn) coll.SolveOnce(ps, dt, grid);
        }

        if (distOn) dist.UpdateRestLengths(ps, dt);

        // 6. Update velocities
        UpdateVelocities(ps, dt);
    }

    // Draws particles and constraints for debugging.
    private void OnDrawGizmos()
    {
        if (ps == null) return;
        if (ps.currentPosition == null) return;

        if (drawParticles)
        {
            for (int i = 0; i < ps.currentPosition.Length; i++)
            {
                Gizmos.color = (selectedParticle != i) ? Color.black : Color.purple;
                Gizmos.DrawSphere(ps.currentPosition[i], .08f);
            }
        }
        
        if (drawDist)
        {
            Gizmos.color = new(1f, 1f, 1f, .5f);
            for (int i = 0; i < dist.constraints.Length; i++)
            {
                if (!dist.constraints[i].active) continue;
                int from = dist.constraints[i].i;
                int to = dist.constraints[i].j;
                Gizmos.DrawLine(ps.currentPosition[from], ps.currentPosition[to]);
            }
        }
        
        Gizmos.color = Color.orange;

        // Draw room bounds
        if (roomArea != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(roomArea.center, roomArea.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, roomDimensions * 2);
            Gizmos.matrix = Matrix4x4.identity;
        }

        // TEMP Draw ball room
        Gizmos.DrawWireSphere(new Vector3(-roomDimensions.x / 2, roomDimensions.y, 0f), roomDimensions.y);
        Gizmos.DrawWireSphere(new Vector3(roomDimensions.x / 2, roomDimensions.y, 0f), roomDimensions.y);


        // Draw colliders
        foreach (CollisionObject obj in coll.objects)
        {
            ICollisionShape shape = obj.shape;

            if (shape is SphereShape sphere) Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            else if (shape is PlaneShape plane) Gizmos.DrawSphere(plane.p0, .1f);
            else if (shape is CapsuleShape capsule)
            {
                Gizmos.DrawWireSphere(capsule.a, capsule.radius);
                Gizmos.DrawWireSphere(capsule.b, capsule.radius);
            }
            else if (shape is BoxShape box) Gizmos.DrawWireCube(box.center, box.b * 2f);
        }

        Gizmos.color = new(0f, 0f, 1f, .5f);
        if (grabbedParticle != -1)
        {
            Gizmos.DrawWireSphere(ps.currentPosition[grabbedParticle], dense.h);
        }

        Gizmos.color = Color.red;
        if (selectedParticle != -1)
        {
            int[] js;
            int jCount;
            (js, jCount) = grid.GetNeighbors(ps, selectedParticle, dense.h);
            for (int iter = 0; iter < jCount; iter++)
            {
                int j = js[iter];
                Gizmos.DrawLine(ps.currentPosition[selectedParticle], ps.currentPosition[j]);
            }
        }
    }
}
