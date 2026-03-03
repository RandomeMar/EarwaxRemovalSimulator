using System;
using System.Collections.Generic;
using System.IO.Pipes;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Receiver.Primitives;

public class XPBDSim : MonoBehaviour
{
    #region
    [Range(1, 30)]
    public int solverIterations;
    public Vector3 gravity = new Vector3(0, -9.8f, 0);

    [Header("Lattice Settings")]
    public Vector3 latticeOrigin = Vector3.zero;
    [Range(2, 30)]
    public int latticeParticleCount = 3;
    [Range(.5f, 30f)]
    public float latticeLength = 2.0f;
    [Range(0f, 1f)]
    public float invMass;

    [Header("Distance Constraint Settings")]
    [Range(0f, 1f)]
    public float distCompliance;
    
    [Header("Density Constraint Settings")]
    public float cellSize = .5f;
    public float restDensity;
    public float denseCompliance;

    #endregion

    const float EPS = 1e-6f;

    ParticleSet ps;
    SpacialHash grid;
    DistanceConstraintSet dist;
    DensityConstraintSolver dense;


    // Classes/Structs:
    class ParticleSet
    {
        public Vector3[] currentPosition;
        public Vector3[] previousPosition;
        public Vector3[] velocity;
        public float[] invMass;

        public int count;

        public ParticleSet(int count)
        {
            this.count = count;
            this.currentPosition = new Vector3[count];
            this.previousPosition = new Vector3[count];
            this.velocity = new Vector3[count];
            this.invMass = new float[count];
        }
    }

    class SpacialHash
    {
        float cellSize;
        Dictionary<long, List<int>> buckets;

        public SpacialHash(float cellSize)
        {
            this.cellSize = cellSize;
            this.buckets = new();
        }

        public Vector3Int CalcCellCoord(Vector3 position)
            {
                return new Vector3Int(
                    Mathf.FloorToInt(position.x / cellSize),
                    Mathf.FloorToInt(position.y / cellSize),
                    Mathf.FloorToInt(position.z / cellSize)
                    );
            }

        public long HashCoord(Vector3Int cellCoord)
        {
            const int SHIFT = 20;

            long x = (long)(cellCoord.x + (1 << SHIFT));
            long y = (long)(cellCoord.y + (1 << SHIFT));
            long z = (long)(cellCoord.z + (1 << SHIFT));

            return (x << 42) | (y << 21) | z;
        }

        public void BuildGrid(ParticleSet ps)
        {
            buckets.Clear();

            for (int i = 0; i < ps.count; i++)
            {
                Vector3Int cellCoord = CalcCellCoord(ps.currentPosition[i]);
                long key = HashCoord(cellCoord);
                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = new List<int>(8); // 8 seems like a good starting value for bucket size
                    buckets[key] = bucket;
                }
                bucket.Add(i);
            }
        }

        public List<int> GetNeighbors(ParticleSet ps, int i)
        {
            Vector3Int baseCell = CalcCellCoord(ps.currentPosition[i]);
            List<int> js = new();
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        Vector3Int c = new(
                            baseCell.x + dx,
                            baseCell.y + dy,
                            baseCell.z + dz
                            );

                        long key = HashCoord(c);

                        if (!buckets.TryGetValue(key, out var bucket)) continue;

                        for (int b = 0; b < bucket.Count; b++)
                        {
                            int j = bucket[b];
                            if (j == i) continue;
                            // For each neighbor j of particle i
                            js.Add(j);
                        }
                    }
            return js;
        }

    }

    interface IConstraintSolver
    {
        void ResetLambda();
        void SolveOnce(ParticleSet ps, float dt, SpacialHash grid);
    }

    struct DistanceConstraint
    {
        public int i, j;
        public float restLength;
        public float compliance;
        public float lambda;

        public DistanceConstraint(int i, int j, float restLength, float compliance, float lambda)
        {
            this.i = i;
            this.j = j;
            this.restLength = restLength;
            this.compliance = compliance;
            this.lambda = lambda;
        }
    }

    class DistanceConstraintSet : IConstraintSolver
    {
        public DistanceConstraint[] constraints;

        public DistanceConstraintSet(DistanceConstraint[] constraints)
        {
            this.constraints = constraints;
        }

        public void ResetLambda()
        {
            for (int i = 0; i < this.constraints.Length; i++)
            {
                this.constraints[i].lambda = 0;
            }
        }

        public void SolveOnce(ParticleSet ps, float dt, SpacialHash grid)
        {
            for (int index = 0; index < this.constraints.Length; index++)
            {
                DistanceConstraint constraint = this.constraints[index];
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
    }

    class DensityConstraintSolver : IConstraintSolver
    {
        public float restDensity;
        public float h;
        public float compliance;
        private Vector3[] deltaX; // Used for Jacobi structure
        private float[] lambda;

        public DensityConstraintSolver(float restDensity, float h, float compliance)
        {
            this.restDensity = restDensity;
            this.h = h;
            this.compliance = compliance;
        }

        private void EnsureCapacity(int count)
        {
            if (this.deltaX == null || this.deltaX.Length != count)
                this.deltaX = new Vector3[count];

            if (this.lambda == null || this.lambda.Length != count)
                this.lambda = new float[count];
        }

        public void ResetLambda()
        {
            if (this.lambda == null) return;
            Array.Clear(this.lambda, 0, this.lambda.Length);
        }

        public void SolveOnce(ParticleSet ps, float dt, SpacialHash grid)
        {
            EnsureCapacity(ps.count);
            Array.Clear(this.deltaX, 0, ps.count);

            float alpha = this.compliance / (dt * dt);

            for (int i = 0; i < ps.count; i++)
            {
                if (ps.invMass[i] <= 0) continue;

                int[] js = grid.GetNeighbors(ps, i).ToArray();

                float density = 0f;
                Vector3 gradi = Vector3.zero;
                float denom = 0f;

                foreach (int j in js)
                {
                    if (ps.invMass[j] <= 0) continue;
                    float mj = 1f / ps.invMass[j];

                    Vector3 distVec = ps.currentPosition[i] - ps.currentPosition[j];

                    density += mj * Poly6(distVec.sqrMagnitude, this.h);
                    Vector3 gradj = -mj * GradPoly6(distVec, this.h);
                    gradi -= gradj;

                    denom += gradj.sqrMagnitude * ps.invMass[j];
                }

                float c = density - this.restDensity;
                denom += gradi.sqrMagnitude * ps.invMass[i];

                // If the denominator is close to 0: continue
                if (denom + alpha <= EPS) continue;

                float deltaLambda = (-c - alpha * this.lambda[i]) / (denom + alpha);
                this.lambda[i] += deltaLambda;

                deltaX[i] += ps.invMass[i] * gradi * deltaLambda;

                foreach (int j in js)
                {
                    if (ps.invMass[j] <= 0) continue;

                    float mj = 1 / ps.invMass[j];
                    Vector3 distVec = ps.currentPosition[i] - ps.currentPosition[j];
                    Vector3 gradj = -mj * GradPoly6(distVec, this.h);

                    deltaX[j] += ps.invMass[j] * gradj * deltaLambda;
                }
            }

            // Jacobi structure
            for (int i = 0; i < ps.count; i++)
            {
                ps.currentPosition[i] += deltaX[i];
            }

        }
    }

    
    // Functions

    // Smoothing kernel for calculating density
    static float Poly6(float r2, float h)
    {
        float h2 = h * h;
        if (r2 < 0 || h2 < r2) return 0;

        return 315 / (64 * Mathf.PI * Mathf.Pow(h, 9)) * Mathf.Pow(h2 - r2, 3);
    }

    // Gradient of Poly6
    static Vector3 GradPoly6(Vector3 rVec, float h)
    {
        float r2 = rVec.sqrMagnitude;
        float h2 = h * h;

        if (r2 <= 0 || h2 < r2) return Vector3.zero;

        return -945 / (32 * Mathf.PI * Mathf.Pow(h, 9)) * Mathf.Pow(h2 - r2, 2) * rVec;
    }


    int calcIndex(int i, int j, int k, int n)
    {
        return n * (n * i + j) + k;
    }

    (ParticleSet, SpacialHash, DistanceConstraintSet, DensityConstraintSolver) GenerateLattice(int n)
    {
        float length = latticeLength;
        float invMass = this.invMass;

        int particleCount = n * n * n;
        float offset = length / (n - 1);

        ParticleSet lattice = new(particleCount);
        SpacialHash grid = new SpacialHash(cellSize);

        List<DistanceConstraint> dist = new();
        DensityConstraintSolver dense = new(restDensity, offset * 1.5f, denseCompliance);

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                for(int k = 0; k < n; k++)
                {
                    // Particles
                    int curr = calcIndex(i, j, k, n);
                    lattice.currentPosition[curr] = new(-length / 2 + i * offset, -length / 2 + j * offset, -length / 2 + k * offset);
                    lattice.currentPosition[curr] += latticeOrigin;


                    lattice.previousPosition[curr] = lattice.currentPosition[curr];
                    lattice.velocity[curr] = new(0, 0, 0);
                    lattice.invMass[curr] = invMass;

                    // Constraints
                    if (i + 1 < n)
                    {
                        int iPlus = calcIndex(i + 1, j, k, n);
                        dist.Add(new DistanceConstraint(curr, iPlus, offset, distCompliance, 0f));

                        if (j + 1 < n)
                        {
                            int jPlus = calcIndex(i, j + 1, k, n);
                            dist.Add(new DistanceConstraint(iPlus, jPlus, Mathf.Sqrt(2) * offset, distCompliance, 0f));
                            int ijPlus = calcIndex(i + 1, j + 1, k, n);
                            dist.Add(new DistanceConstraint(curr, ijPlus, Mathf.Sqrt(2) * offset, distCompliance, 0f));
                        }

                    }
                    if (j + 1 < n)
                    {
                        int jPlus = calcIndex(i, j + 1, k, n);
                        dist.Add(new DistanceConstraint(curr, jPlus, offset, distCompliance, 0f));

                        if (k + 1 < n)
                        {
                            int kPlus = calcIndex(i, j, k + 1, n);
                            dist.Add(new DistanceConstraint(jPlus, kPlus, Mathf.Sqrt(2) * offset, distCompliance, 0f));
                            int jkPlus = calcIndex(i, j + 1, k + 1, n);
                            dist.Add(new DistanceConstraint(curr, jkPlus, Mathf.Sqrt(2) * offset, distCompliance, 0f));
                        }
                    }
                    if (k + 1 < n)
                    {
                        int kPlus = calcIndex(i, j, k + 1, n);
                        dist.Add(new DistanceConstraint(curr, kPlus, offset, distCompliance, 0f));

                        if (i + 1 < n)
                        {
                            int iPlus = calcIndex(i + 1, j, k, n);
                            dist.Add(new DistanceConstraint(kPlus, iPlus, Mathf.Sqrt(2) * offset, distCompliance, 0f));
                            int kiPlus = calcIndex(i + 1, j, k + 1, n);
                            dist.Add(new DistanceConstraint(curr, kiPlus, Mathf.Sqrt(2) * offset, distCompliance, 0f));
                        }
                    }
                }
            }
        }
        DistanceConstraintSet dcs = new(dist.ToArray());

        return (lattice, grid, dcs, dense);
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
    
    void SolveFloorCollision(ParticleSet ps)
    {
        float floorValue = 0;

        for (int i = 0; i < ps.currentPosition.Length; i++)
        {
            if (ps.currentPosition[i].y < floorValue)
            {
                ps.currentPosition[i].y = floorValue;
                ps.velocity[i].y = 0;
            }
        }
    }
    
    void UpdateVelocities(ParticleSet ps, float dt)
    {
        for (int i = 0; i < ps.velocity.Length; i++)
        {
            if (ps.invMass[i] == 0) continue; // Particles with invMass 0 should not move
            ps.velocity[i] = (ps.currentPosition[i] - ps.previousPosition[i]) / dt;
        }
    }

    private int grabbedParticle = -1;
    Plane dragPlane;
    private int selectedParticle = -1;

    // Mouse interaction functions
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


    private void Start()
    {
        // Initialize particle set and constraint set.
        (ps, grid, dist, dense) = GenerateLattice(latticeParticleCount);
    }


    private void Update()
    {
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

    // Sim Loop
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 1. Reset lambda
        dist.ResetLambda();
        dense.ResetLambda();

        // 2. Apply external forces
        ApplyForces(ps, dt);

        // 3. Predict positions
        PredictPositions(ps, dt);

        // 4. Build spatial grid
        grid.BuildGrid(ps);

        // 5. Solve constraints
        for (int i = 0; i < solverIterations; i++)
        {
            dist.SolveOnce(ps, dt, grid);
            dense.SolveOnce(ps, dt, grid);
            SolveFloorCollision(ps);
        }

        // 6. Update velocities
        UpdateVelocities(ps, dt);
    }

    

    // Draws particles and constraints for debugging.
    private void OnDrawGizmos()
    {
        if (ps == null) return;
        if (ps.currentPosition == null) return;

        for (int i = 0; i < ps.currentPosition.Length; i++)
        {
            Gizmos.color = (selectedParticle != i) ? Color.black : Color.purple;
            Gizmos.DrawSphere(ps.currentPosition[i], .08f);
        }

        for (int i = 0; i < dist.constraints.Length; i++)
        {
            int from = dist.constraints[i].i;
            int to = dist.constraints[i].j;
            Gizmos.color = new(1f, 1f, 1f, .5f);
            Gizmos.DrawLine(ps.currentPosition[from], ps.currentPosition[to]);
        }

        if (grabbedParticle != -1)
        {
            Gizmos.color = new(0f, 0f, 1f, .5f);
            Gizmos.DrawWireSphere(ps.currentPosition[grabbedParticle], dense.h);
        }

        if (selectedParticle != -1)
        {
            var js = grid.GetNeighbors(ps, selectedParticle);
            foreach (int j in js)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(ps.currentPosition[selectedParticle], ps.currentPosition[j]);
            }
        }
    }
}
