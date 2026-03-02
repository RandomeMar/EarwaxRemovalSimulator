using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class XPBDSim : MonoBehaviour
{
    const float eps = 1e-6f;

    [SerializeField]
    [Range(1, 30)]
    int solverIterations;

    [SerializeField]
    [Range(2, 30)]
    int latticeParticleCount = 3;

    [SerializeField]
    Vector3 gravity = new Vector3(0, -9.8f, 0);

    [SerializeField]
    Vector3 latticeOrigin = Vector3.zero;

    [SerializeField]
    [Range(0f, 1f)]
    float compliance;

    [SerializeField]
    [Range(0f, 1f)]
    float w;

    ParticleSet ps;
    DistanceConstraintSet dist;

    /// <summary>
    /// Container for particle data stored in arrays.
    /// </summary>
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

    /// <summary>
    /// Represents a single XPBD distance constraint between two particles.
    /// </summary>
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

    /// <summary>
    /// Collection of distance constraints stored in an array.
    /// </summary>
    class DistanceConstraintSet
    {
        public DistanceConstraint[] constraints;

        public DistanceConstraintSet(DistanceConstraint[] constraints)
        {
            this.constraints = constraints;
        }

        /// <summary>
        /// Solves a single iteration of all distance constraints in the set.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="dt"></param>
        public void SolveOnce(ParticleSet ps, float dt)
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
                if (l <= eps) { continue; }

                Vector3 n = d / l; // Normalized direction from j to i

                float alpha = constraint.compliance / (dt * dt);

                // If the denominator is close to 0: continue
                float denom = (ps.invMass[i] + ps.invMass[j] + alpha);
                if (denom <= eps) { continue; }

                float deltaLambda = (-c - alpha * constraint.lambda) / denom;

                constraint.lambda += deltaLambda;
                this.constraints[index] = constraint;

                ps.currentPosition[i] += ps.invMass[i] * n * deltaLambda;
                ps.currentPosition[j] -= ps.invMass[j] * n * deltaLambda;
            }
        }
    }


    /// <summary>
    /// Calculates the index of a particle in a latice particle set.
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <param name="k"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    int calcIndex(int i, int j, int k, int n)
    {
        return n * (n * i + j) + k;
    }

    /// <summary>
    /// Generates a (n x n x n) lattice of particles and distance constraints.
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    (ParticleSet, DistanceConstraintSet) GenerateLattice(int n)
    {
        float length = 2f;
        float invMass = w;

        int particleCount = n * n * n;
        float offset = length / (n - 1);

        ParticleSet lattice = new(particleCount);
        List<DistanceConstraint> constraints = new();

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
                        constraints.Add(new DistanceConstraint(curr, iPlus, offset, compliance, 0f));

                        if (j + 1 < n)
                        {
                            int jPlus = calcIndex(i, j + 1, k, n);
                            constraints.Add(new DistanceConstraint(iPlus, jPlus, Mathf.Sqrt(2) * offset, compliance, 0f));
                            int ijPlus = calcIndex(i + 1, j + 1, k, n);
                            constraints.Add(new DistanceConstraint(curr, ijPlus, Mathf.Sqrt(2) * offset, compliance, 0f));
                        }

                    }
                    if (j + 1 < n)
                    {
                        int jPlus = calcIndex(i, j + 1, k, n);
                        constraints.Add(new DistanceConstraint(curr, jPlus, offset, compliance, 0f));

                        if (k + 1 < n)
                        {
                            int kPlus = calcIndex(i, j, k + 1, n);
                            constraints.Add(new DistanceConstraint(jPlus, kPlus, Mathf.Sqrt(2) * offset, compliance, 0f));
                            int jkPlus = calcIndex(i, j + 1, k + 1, n);
                            constraints.Add(new DistanceConstraint(curr, jkPlus, Mathf.Sqrt(2) * offset, compliance, 0f));
                        }
                    }
                    if (k + 1 < n)
                    {
                        int kPlus = calcIndex(i, j, k + 1, n);
                        constraints.Add(new DistanceConstraint(curr, kPlus, offset, compliance, 0f));

                        if (i + 1 < n)
                        {
                            int iPlus = calcIndex(i + 1, j, k, n);
                            constraints.Add(new DistanceConstraint(kPlus, iPlus, Mathf.Sqrt(2) * offset, compliance, 0f));
                            int kiPlus = calcIndex(i + 1, j, k + 1, n);
                            constraints.Add(new DistanceConstraint(curr, kiPlus, Mathf.Sqrt(2) * offset, compliance, 0f));
                        }
                    }
                }
            }
        }
        DistanceConstraintSet dcs = new(constraints.ToArray());
        //lattice.invMass[calcIndex(n - 1, n - 1, n - 1, n)] = 0; // NOTE: This is only for testing

        return (lattice, dcs);
    }

    /// <summary>
    /// Updates a particle set's velocities based on gravity.
    /// </summary>
    /// <param name="ps"></param>
    /// <param name="dt"></param>
    void ApplyForces(ParticleSet ps, float dt)
    {
        for (int i = 0; i < ps.velocity.Length; i++)
        {
            if (ps.invMass[i] == 0) continue; // Particles with invMass 0 should not move
            ps.velocity[i] += gravity * dt;
        }
    }

    /// <summary>
    /// Calculates new particle positions based on particle velocity.
    /// </summary>
    /// <param name="ps"></param>
    /// <param name="dt"></param>
    void PredictPositions(ParticleSet ps, float dt)
    {
        for (int i = 0; i < ps.velocity.Length; i++)
        {
            if (ps.invMass[i] == 0) continue; // Particles with invMass 0 should not move
            ps.previousPosition[i] = ps.currentPosition[i];
            ps.currentPosition[i] += ps.velocity[i] * dt;
        }
    }

    /// <summary>
    /// Clamps particle positions to a floor value.
    /// </summary>
    /// <param name="ps"></param>
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

    /// <summary>
    /// Updates particle velocities based on the amount a particle moved in delta time.
    /// </summary>
    /// <param name="ps"></param>
    /// <param name="dt"></param>
    void UpdateVelocities(ParticleSet ps, float dt)
    {
        for (int i = 0; i < ps.velocity.Length; i++)
        {
            if (ps.invMass[i] == 0) continue; // Particles with invMass 0 should not move
            ps.velocity[i] = (ps.currentPosition[i] - ps.previousPosition[i]) / dt;
        }
    }

    private int selectedParticle = -1;
    Plane dragPlane;

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

            // Hard drag (most stable for testing)
            ps.currentPosition[selectedIndex] = target;
            ps.previousPosition[selectedIndex] = target;
            ps.velocity[selectedIndex] = Vector3.zero;
        }
    }

    private void Start()
    {
        // Initialize particle set and constraint set.
        (ps, dist) = GenerateLattice(latticeParticleCount);
    }


    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            selectedParticle = -1;
            return;
        }

        if (selectedParticle != -1)
        {
            BeginDrag(selectedParticle);
            DragUpdate(selectedParticle);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            selectedParticle = SelectParticle();
        }
    }

    // Sim Loop
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Reset lambda
        for (int i = 0; i < dist.constraints.Length; i++)
        {
            dist.constraints[i].lambda = 0f;
        }

        // 1. Apply external forces
        ApplyForces(ps, dt);

        // 2. Predict positions
        PredictPositions(ps, dt);

        // 3. Solve constraints
        for (int i = 0; i < solverIterations; i++)
        {
            dist.SolveOnce(ps, dt);
            SolveFloorCollision(ps);
        }

        // 4. Update velocities
        UpdateVelocities(ps, dt);
    }

    

    // Draws particles and constraints for debugging.
    private void OnDrawGizmos()
    {
        if (ps == null) return;
        if (ps.currentPosition == null) return;

        for (int i = 0; i < ps.currentPosition.Length; i++)
        {
            Gizmos.DrawSphere(ps.currentPosition[i], .08f);
        }
        
        for (int i = 0; i < dist.constraints.Length; i++)
        {
            int from = dist.constraints[i].i;
            int to = dist.constraints[i].j;
            Gizmos.DrawLine(ps.currentPosition[from], ps.currentPosition[to]);
        }

    }
}
