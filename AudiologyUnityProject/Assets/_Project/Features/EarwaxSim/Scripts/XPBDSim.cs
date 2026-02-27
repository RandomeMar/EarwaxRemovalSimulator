using UnityEngine;
using UnityEngine.UIElements;

public class XPBDSim : MonoBehaviour
{

    [SerializeField]
    int solverIterations;
    Vector3 gravity = new Vector3(0, -9.8f, 0);

    const float eps = 1e-6f;

    ParticleSet ps;
    DistanceConstraintSet dist;

    class ParticleSet
    {
        public Vector3[] currentPosition;
        public Vector3[] previousPosition;
        public Vector3[] velocity;
        public float[] invMass;
    }

    struct DistanceConstraint
    {
        public int i, j;
        public float restLength;
        public float compliance;
        public float lambda;
    }

    class DistanceConstraintSet
    {
        DistanceConstraint[] constraints;

        public void SolveOnce(ParticleSet ps, float dt)
        {
            for (int iter = 0; iter < this.constraints.Length; iter++)
            {
                DistanceConstraint constraint = this.constraints[iter];
                int i = constraint.i;
                int j = constraint.j;
                Vector3 d = ps.currentPosition[i] - ps.currentPosition[j]; // Vector from j to i
                float l = d.magnitude; // Distance between i and j
                float c = l - constraint.restLength; // C

                // if l is close to 0: continue
                if (l < eps) { continue; }

                Vector3 n = d / l; // Normalized direction from j to i

                float alpha = constraint.compliance / (dt * dt);

                float deltaLambda = (-c - alpha * constraint.lambda) / (ps.invMass[i] + ps.invMass[j] + alpha);

                constraint.lambda += deltaLambda;
                this.constraints[iter] = constraint;

                ps.currentPosition[i] += ps.invMass[i] * n * deltaLambda;
                ps.currentPosition[j] -= ps.invMass[j] * n * deltaLambda;
            }
        }
    }


    void ApplyForces(ParticleSet ps, float dt)
    {
        for (int i = 0; i < ps.velocity.Length; i++)
        {
            ps.velocity[i] += gravity * dt;
        }
    }

    void PredictPositions(ParticleSet ps, float dt)
    {
        for (int i = 0; i < ps.velocity.Length; i++)
        {
            ps.previousPosition[i] = ps.currentPosition[i];
            ps.currentPosition[i] += ps.velocity[i] * dt;
        }
    }

    void SolveFloorCollision()
    {
        // TODO: For all positions, if y is below some floor value, reset to the floor value
    }

    void UpdateVelocities()
    {
        // TODO: v = (current position - previous position) / dt
    }

    private void Start()
    {
        // Spawn in particles
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 1. Apply external forces
        ApplyForces(ps, dt);

        // 2. Predict positions
        PredictPositions(ps, dt);

        // 3. Solve constraints
        for (int i = 0; i < solverIterations; i++)
        {
            dist.SolveOnce(ps, dt);
            SolveFloorCollision();
        }

        // 4. Update velocities
        UpdateVelocities();
    }
}
