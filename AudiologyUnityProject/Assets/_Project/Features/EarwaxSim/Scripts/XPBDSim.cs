using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace EarwaxSim
{
    // Owns the full XPBD loop
    public class XPBDSim : MonoBehaviour
    {
        #region Public Parameters
        [Header("Haptic Manager")]
        public NewHapticManager hapticManager;

        [Header("Collision Objects")]
        public DynamicCollisionObject toolObj;
        public CollisionObjectBase roomObj;

        [Header("Gizmo Debug Settings")]
        public bool drawParticles = true;
        [Min(0)]
        public float particleViewRadius = .5f;
        public bool drawDist = true;
        public bool drawAdhes = true;

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
        [Min(0f)]
        public float particleRadius = .5f;

        [Header("Material Settings")]
        [Min(0f)]
        public float materialDensity;
        [Min(0f)]
        public float baseBondCompliance; // NOTE: Although distance constraint compliance is based on lattice scale, it does not scale perfectly with it.
        // This means compliance will need to be adjusted when the lattice is scaled.

        [Header("Adhesion Constraint Settings")]
        public bool adhesOn;

        [Header("Distance Constraint Settings")]
        public bool distOn = true;
        public float yieldStrain = .5f; // At what strain distance constraints begin permanently deforming
        public float plasticFlow = 1f; // The rate that distance constraints permanently deform
        public float breakStrain = 1f; // At what strain distance constraints break

        [Header("Visco-elasticity Settings")]
        [Min(0f)]
        public float adaptRate; // How much distance constraints give based on temporary deformation
        [Min(0f)]
        public float recoveryRate; // How quickly particles return to their original position

        [Header("Density Constraint Settings")]
        public bool denseOn = true;
        [Min(0f)]
        public float denseCompliance;
        [Min(1f)]
        public float hMult = 1.25f; // Distance that density constraints are applied. Scales with particle spacing

        [Header("Collision Constraint Settings")]
        public bool collOn = true;
        public bool colliderCollOn = true; // Whether collider vs. collider collisions happen
        [Min(0f)]
        public float collCompliance;
        #endregion

        #region Solver Objects
        public ParticleSet ps;
        SpatialHash grid;
        AdhesionConstraint[] anchors;

        // Solvers
        DistanceConstraintSet dist;
        DensityConstraintSolver dense;
        CollisionConstraintSolver coll;
        AdhesionConstraintSolver adhes;
        #endregion


        // ------ Functions ------

        // Smoothing kernel for calculating density
        static float Poly6(float r2, float h)
        {
            float h2 = h * h;
            if (h2 < r2) return 0;

            float h4 = h2 * h2;

            float term = h2 - r2;

            return 315 / (64 * Mathf.PI * h4 * h4 * h) * term * term * term;
        }

        // Estimates rest density of particles in a lattice. NOTE: Needs to be different if particle set is not a lattice
        float CalcRestDensity(ParticleSet ps, SpatialHash grid, float h)
        {
            int n = latticeParticleCount;
            int i = CalcIndex(n / 2, n / 2, n / 2, n);
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

        // Calculates index in a 1d array of particles based on a x, y, z coordinate in a 3d lattice
        int CalcIndex(int x, int y, int z, int n)
        {
            return n * (n * x + y) + z;
        }

        // Builds a particle set that is a lattice along with all necessary constraint solvers for the lattice
        (ParticleSet, SpatialHash, DistanceConstraintSet, DensityConstraintSolver) GenerateLattice()
        {
            int n = latticeParticleCount; // Number of particles in a single row/column
            int particleCount = n * n * n; // Total particle count
            float length = latticeLength;
            float spacing = length / (n - 1);
            float h = spacing * hMult; // Distance that density constraint looks for neighbors. Higher means more neighbors to search through

            float totalVolume = length * length * length;
            float totalMass = materialDensity * totalVolume;
            float particleMass = totalMass / particleCount;
            float invMass = 1f / particleMass;
            float bondCompliance = baseBondCompliance * spacing;

            ParticleSet lattice = new(particleCount, particleRadius * (spacing / 2f));
            List<DistanceConstraint> dist = new();

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    for (int k = 0; k < n; k++)
                    {
                        // ------ Particles ------
                        int curr = CalcIndex(i, j, k, n);
                        lattice.currentPosition[curr] = new(-length / 2 + i * spacing, -length / 2 + j * spacing, -length / 2 + k * spacing);
                        lattice.currentPosition[curr] += latticeOrigin;


                        lattice.previousPosition[curr] = lattice.currentPosition[curr];
                        lattice.velocity[curr] = new(0, 0, 0);
                        lattice.invMass[curr] = invMass;
                        lattice.mass[curr] = particleMass;

                        // ------ Constraints ------
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

            DistanceConstraintSet dcs = new(dist.ToArray(), yieldStrain, plasticFlow, breakStrain, adaptRate, recoveryRate);

            SpatialHash grid = new SpatialHash(h, particleCount);

            grid.BuildGrid(lattice);

            float restDensity = CalcRestDensity(lattice, grid, h);

            DensityConstraintSolver dense = new(restDensity, h, denseCompliance);

            print("GENERATED LATTICE!!!");

            return (lattice, grid, dcs, dense);
        }

        // Initializes particle set and constraint solvers
        void BuildSimulation()
        {
            (ps, grid, dist, dense) = GenerateLattice();

            anchors = new AdhesionConstraint[ps.count];
            adhes = new AdhesionConstraintSolver(anchors);
            coll = new CollisionConstraintSolver(collCompliance, anchors);
        }

        // Updates particle velocity based on gravity
        void ApplyForces(ParticleSet ps, float dt)
        {
            for (int i = 0; i < ps.velocity.Length; i++)
            {
                if (ps.invMass[i] == 0) continue; // Particles with invMass 0 should not move
                ps.velocity[i] += gravity * dt;
            }
        }

        // Updates particle positions without taking into account collisions or other constraints
        void PredictPositions(ParticleSet ps, float dt)
        {
            for (int i = 0; i < ps.velocity.Length; i++)
            {
                if (ps.invMass[i] == 0) continue; // Particles with invMass 0 should not move
                ps.previousPosition[i] = ps.currentPosition[i];
                ps.currentPosition[i] += ps.velocity[i] * dt;
            }
        }

        // Updates velocities based on change in position on the current frame
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

        // Finds closest particle to mouse position and returns its index
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

        // Creates a drag plane aligned with the selected particle
        Plane GetDragPlane(int selectedIndex)
        {
            // Get drag plane
            Vector3 planeNormal = Camera.main.transform.forward;
            Vector3 planePoint = ps.currentPosition[selectedIndex];

            return new(planeNormal, planePoint);
        }

        // Drags particle across drag plane based on mouse movement
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
            BuildSimulation();
        }

        // Called before the first frame
        private void Start()
        {
            // TODO: Remove when coll.objects is deprecated
            coll.objects.Add(toolObj);
            coll.objects.Add(roomObj);

            coll.tool = toolObj;
            coll.canal = roomObj;
        }

        // User input loop
        private void Update()
        {
            // Mouse manipulation
             if (Input.GetMouseButtonUp(0))
            {
                grabbedParticle = -1;
                return;
            }

            if (grabbedParticle != -1)
            {
                DragUpdate(grabbedParticle);
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                grabbedParticle = SelectParticle();
                if (grabbedParticle != -1)
                {
                    dragPlane = GetDragPlane(grabbedParticle);
                }
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
            float collAlpha = coll.compliance / (dt * dt);

            // ------ 1. Reset lambda ------
            if (distOn) dist.ResetLambda();
            if (denseOn) dense.ResetLambda();
            if (adhesOn) adhes.ResetLambda();
            if (collOn) coll.ResetLambda(); // Also resets haptic message

            // ------ 2. Apply external forces ------
            ApplyForces(ps, dt);

            // ------ 3. Predict positions ------
            PredictPositions(ps, dt);

            if (toolObj != null)
            {
                toolObj.previousPosition = toolObj.transform.position;
                toolObj.MoveTool(dt);
            }

            // ------ 4. Build spatial grid. ------
            if (denseOn) grid.BuildGrid(ps);

            // ------ 5. Solve constraints ------
            coll.SolveColliderCollider(collAlpha); // Run tool vs. canal collisions only once per frame for stability

            for (int i = 0; i < solverIterations; i++)
            {
                if (distOn) dist.SolveOnce(ps, dt, grid);
                if (denseOn) dense.SolveOnce(ps, dt, grid);
                if (collOn) coll.NewSolveOnce(ps, dt); // NOTE: This does not solve particle vs. canal collisions
                if (adhesOn) adhes.SolveOnce(ps, dt, grid);
            }

            coll.SolvePSCollider(ps, coll.canal, collAlpha); // Run canal collisions only once per frame for performance

            if (distOn) dist.UpdateRestLengths(ps, dt); // Update rest lengths after solving

            // ------ 6. Update velocities ------
            UpdateVelocities(ps, dt);
            toolObj.velocity = (toolObj.transform.position - toolObj.previousPosition) / dt;

            // ------ 7. Send HapticMessage to NewHapticManager from the collision solver ------
            hapticManager.SetHapticMessage(coll.GetHapticMessage());
        }

        // Draws particles and constraints for debugging.
        private void OnDrawGizmos()
        {
            if (ps == null) return;
            if (ps.currentPosition == null) return;

            // Draw particles
            Gizmos.color = Color.black;
            for (int i = 0; i < ps.currentPosition.Length; i++)
            {
                if (drawParticles) Gizmos.DrawSphere(ps.currentPosition[i], ps.radius * particleViewRadius);

                // Draw adhesion constraints
                if (adhesOn && anchors[i].isActive && drawAdhes)
                {
                    Gizmos.color = Color.green;

                    Vector3 anchorPos = anchors[i].shape.GetWorldPos(anchors[i].localAnchorPos);

                    Gizmos.DrawSphere(anchorPos, .05f);

                    Gizmos.DrawLine(
                        ps.currentPosition[i],
                        anchorPos);
                    Gizmos.color = Color.black;
                }
            }

            // Draw distance constraints
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

            // Draw grabbed particle
            Gizmos.color = new(0f, 0f, 1f, .5f);
            if (grabbedParticle != -1)
            {
                Gizmos.DrawWireSphere(ps.currentPosition[grabbedParticle], dense.h);
            }

            // Draw selected particle's density constraints
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
}
