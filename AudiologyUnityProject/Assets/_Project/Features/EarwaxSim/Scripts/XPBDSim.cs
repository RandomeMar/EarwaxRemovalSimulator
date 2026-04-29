using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace EarwaxSim
{
    /// <summary>
    /// Manager of the XPBD simulation loop.
    /// </summary>
    /// <remarks>This class is responsible for getting reference to necessary game objects, initializing the simulation, and running the simulation each FixedUpdate frame.</remarks>
    public class XPBDSim : MonoBehaviour
    {
        #region Public Parameters
        [Header("Haptic Manager")]
        public NewHapticManager hapticManager;

        [Header("Collision Objects")]
        public DynamicCollisionObject tool;
        public CollisionObjectBase canal;

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
        [Min(0)]
        public float particleDeleteRadius = 10f;

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

        [Header("Procedural Earwax Shape")]
        [Tooltip("Use procedural noise-based shape instead of uniform lattice.")]
        public bool useProceduralShape = false;
        [Tooltip("Seed for reproducible shapes. Same seed = same earwax.")]
        public int proceduralSeed = 42;
        [Tooltip("Base ellipsoid radii (x, y, z) before noise displacement.")]
        public Vector3 proceduralRadii = new Vector3(0.5f, 0.3f, 0.4f);
        [Tooltip("How much noise displaces the surface (fraction of radius).")]
        [Range(0f, 0.8f)]
        public float proceduralNoiseStrength = 0.3f;
        [Tooltip("Noise frequency. Higher = more bumps.")]
        [Range(0.5f, 8f)]
        public float proceduralNoiseFrequency = 2.0f;
        [Tooltip("Number of noise octaves for detail.")]
        [Range(1, 5)]
        public int proceduralNoiseOctaves = 3;

        [Header("Wax Preset")]
        [Tooltip("Quick presets for different wax types. Custom = keep inspector values as-is; " +
                 "any other preset overwrites material + noise fields when the sim builds.")]
        public WaxPreset waxPreset = WaxPreset.Custom;

        [Header("Procedural Parametric Ranges")]
        [Tooltip("If on, the seed also rolls the ACTUAL radii/noise values from the ranges below. " +
                 "Each seed then produces a different silhouette — tall/skinny/bumpy vs. wide/smooth, etc.")]
        public bool useParametricRanges = false;
        [Tooltip("Per-axis minimum radius when parametric ranges are on.")]
        public Vector3 proceduralRadiiMin = new Vector3(0.3f, 0.2f, 0.3f);
        [Tooltip("Per-axis maximum radius when parametric ranges are on.")]
        public Vector3 proceduralRadiiMax = new Vector3(0.6f, 0.5f, 0.5f);
        [Tooltip("Min/max noise strength (x=min, y=max).")]
        public Vector2 proceduralNoiseStrengthRange = new Vector2(0.15f, 0.45f);
        [Tooltip("Min/max noise frequency (x=min, y=max).")]
        public Vector2 proceduralNoiseFrequencyRange = new Vector2(1.5f, 3.5f);

        [Header("Procedural Asymmetry / Lobes")]
        [Tooltip("Pushes noise bumps toward one side of the blob. 0 = uniform, 1 = fully biased.")]
        [Range(0f, 1f)]
        public float proceduralBumpinessBias = 0f;
        [Tooltip("Direction the bumpiness gets biased toward.")]
        public Vector3 proceduralBumpinessBiasAxis = Vector3.up;
        [Tooltip("Low-frequency sine warp magnitude. Bends/lobes the overall shape. 0 = straight.")]
        [Range(0f, 1f)]
        public float proceduralStretchAmount = 0f;
        [Tooltip("Sine wave frequency for the stretch warp.")]
        public float proceduralStretchFrequency = 3f;
        [Tooltip("Flattens one side of the blob, as if pressed against the canal wall. 0 = none, 1 = fully flat.")]
        [Range(0f, 1f)]
        public float proceduralSquashAmount = 0f;
        [Tooltip("Direction that gets squashed. Default -Y = flatten the bottom.")]
        public Vector3 proceduralSquashAxis = Vector3.down;
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

        /// <summary>
        /// Smoothing kernel for calculating density.
        /// </summary>
        /// <param name="r2">Squared distance between two particles.</param>
        /// <param name="h">Smoothing radius.</param>
        /// <returns>Density contibution percentage.</returns>
        static float Poly6(float r2, float h)
        {
            float h2 = h * h;
            if (h2 < r2) return 0;

            float h4 = h2 * h2;

            float term = h2 - r2;

            return 315 / (64 * Mathf.PI * h4 * h4 * h) * term * term * term;
        }

        /// <summary>
        /// Estimates rest density of particles in a lattice.
        /// </summary>
        /// <param name="ps">Particle set housing the lattice.</param>
        /// <param name="grid">Spatial hash grid storing neighboring particles.</param>
        /// <param name="h">Smoothing radius.</param>
        /// <returns>Estimated rest density for all particles in the lattice.</returns>
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

        /// <summary>
        /// Calculates a particle's index in its particle set based on its position in a 3D lattice.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="z">Z coordinate.</param>
        /// <param name="n">Number of particles per row in the lattice.</param>
        /// <returns>Index for the particle set.</returns>
        int CalcIndex(int x, int y, int z, int n)
        {
            return n * (n * x + y) + z;
        }

        /// <summary>
        /// Builds a lattice particle set along with all necessary constraint solvers for the lattice
        /// </summary>
        /// <returns>The lattice particle set, a spatial hash grid, a distance constraint solver, and a density solver.</returns>
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


        // Feature 3: Wax presets. Overwrites material + noise params with canned values for
        // different wax types so the sponsor demo has easy variety. Custom = no-op.
        void ApplyWaxPreset()
        {
            switch (waxPreset)
            {
                case WaxPreset.DryCrumbly:
                    // Hard, brittle, breaks apart under light pressure.
                    materialDensity = 1.2f;
                    yieldStrain = 0.1f;   // yields almost immediately
                    plasticFlow = 0.3f;   // very little flow
                    breakStrain = 0.3f;   // shatters
                    proceduralNoiseStrength = 0.45f;
                    proceduralNoiseFrequency = 3.5f;
                    proceduralNoiseOctaves = 4;
                    break;

                case WaxPreset.SoftSticky:
                    // Pliable, stretches, hard to break.
                    materialDensity = 0.8f;
                    yieldStrain = 0.8f;   // stretches a lot before yielding
                    plasticFlow = 1.2f;   // flows easily
                    breakStrain = 2.0f;   // very hard to break
                    proceduralNoiseStrength = 0.2f;
                    proceduralNoiseFrequency = 1.5f;
                    proceduralNoiseOctaves = 2;
                    break;

                case WaxPreset.OldImpacted:
                    // Dense, stuck, moderately stiff — the "hard case" the student has to work for.
                    materialDensity = 1.5f;
                    yieldStrain = 0.3f;
                    plasticFlow = 0.5f;
                    breakStrain = 0.8f;
                    proceduralNoiseStrength = 0.35f;
                    proceduralNoiseFrequency = 2.5f;
                    proceduralNoiseOctaves = 3;
                    break;

                case WaxPreset.Custom:
                default:
                    // Leave everything alone.
                    break;
            }
        }


        /// <summary>
        /// Initializes particle set and constraint solvers.
        /// </summary>
        void BuildSimulation()
        {
            // Apply wax preset first so procedural/material fields reflect the selected type.
            ApplyWaxPreset();

            if (useProceduralShape) // If true, generates a procedural earwax shape instead of a uniform lattice. See ProceduralEarwax.cs for details.
            {
                var gen = new ProceduralEarwax
                {
                    seed = proceduralSeed,
                    baseRadii = proceduralRadii,
                    resolution = latticeParticleCount,
                    origin = latticeOrigin,
                    noiseStrength = proceduralNoiseStrength,
                    noiseFrequency = proceduralNoiseFrequency,
                    noiseOctaves = proceduralNoiseOctaves,
                    materialDensity = materialDensity,
                    baseBondCompliance = baseBondCompliance,
                    hMult = hMult,
                    denseCompliance = denseCompliance,
                    yieldStrain = yieldStrain,
                    plasticFlow = plasticFlow,
                    breakStrain = breakStrain,
                    adaptRate = adaptRate,
                    recoveryRate = recoveryRate,

                    // Feature 1: parametric ranges
                    useParametricRanges = useParametricRanges,
                    radiiMin = proceduralRadiiMin,
                    radiiMax = proceduralRadiiMax,
                    noiseStrengthRange = proceduralNoiseStrengthRange,
                    noiseFrequencyRange = proceduralNoiseFrequencyRange,

                    // Feature 2: asymmetry / lobes
                    bumpinessBias = proceduralBumpinessBias,
                    bumpinessBiasAxis = proceduralBumpinessBiasAxis,
                    stretchAmount = proceduralStretchAmount,
                    stretchFrequency = proceduralStretchFrequency,
                    squashAmount = proceduralSquashAmount,
                    squashAxis = proceduralSquashAxis,
                };
                (ps, grid, dist, dense) = gen.Generate();
            }
            else
            {
                (ps, grid, dist, dense) = GenerateLattice();
            }

            anchors = new AdhesionConstraint[ps.maxCount];
            adhes = new AdhesionConstraintSolver(anchors);
            coll = new CollisionConstraintSolver(collCompliance, anchors);
        }
        

        public float GetPercentWaxRemoved()
        {
            if (ps == null) return 0.0f;
            return (ps.count / (float)ps.maxCount) * 100f; // NOTE: It might make more sense if this is a method of the ParticleSet class

            // NOTE: The problem with this was that it gets the percentage if distance constraints removed. The earwax is actually the particles.

            //if (dist == null || dist.constraints == null)
            //    return 0f;

            //int total = dist.constraints.Length;
            //int total = ps.active.Length;
            //int broken = 0;

            //for (int i = 0; i < total; i++)
            //{
            //    if (!dist.constraints[i].active)
            //        broken++;
            //}

            //return (broken / (float)total) * 100f;
        }
        

        /// <summary>
        /// Updates particle velocities based on gravity.
        /// </summary>
        /// <param name="ps">Input particle set</param>
        /// <param name="dt">Delta time.</param>
        void ApplyForces(ParticleSet ps, float dt)
        {
            for (int i = 0; i < ps.velocity.Length; i++)
            {
                if (!ps.active[i]) continue; // Ignore not active particles
                if (ps.invMass[i] == 0) continue; // Particles with invMass 0 should not move
                ps.velocity[i] += gravity * dt;
            }
        }

        /// <summary>
        /// Updates particle positions without taking into account collisions or other constraints.
        /// </summary>
        /// <param name="ps">Input particle set.</param>
        /// <param name="dt">Delta time.</param>
        void PredictPositions(ParticleSet ps, float dt)
        {
            for (int i = 0; i < ps.velocity.Length; i++)
            {
                if (!ps.active[i]) continue; // Ignore not active particles
                if (ps.invMass[i] == 0) continue; // Particles with invMass 0 should not move
                ps.previousPosition[i] = ps.currentPosition[i];
                ps.currentPosition[i] += ps.velocity[i] * dt;
            }
        }

        /// <summary>
        /// Updates particle velocities based on change in position on the current frame.
        /// </summary>
        /// <param name="ps">Input particle set.</param>
        /// <param name="dt">Delta time.</param>
        void UpdateVelocities(ParticleSet ps, float dt)
        {
            for (int i = 0; i < ps.velocity.Length; i++)
            {
                if (!ps.active[i]) continue; // Ignore not active particles
                if (ps.invMass[i] == 0) continue; // Particles with invMass 0 should not move
                ps.velocity[i] = (ps.currentPosition[i] - ps.previousPosition[i]) / dt;

                ps.velocity[i] *= globalDamping; // Velocity damping
            }
        }

        // Deactivates particles outside of the particleDeleteRadius
        void RemoveParticles(ParticleSet ps)
        {
            for (int i = 0; i < ps.maxCount; i++)
            {
                if (!ps.active[i]) continue;

                Vector3 distVec = ps.currentPosition[i] - this.transform.position; // Vector from XPBDSim origin to particle position
                if (distVec.magnitude >= this.particleDeleteRadius) ps.active[i] = false;
            }
        }

        #region Mouse Interaction Functions
        private int grabbedParticle = -1;
        Plane dragPlane;
        private int selectedParticle = -1;

        /// <summary>
        /// Finds closest particle to mouse position and returns its index.
        /// </summary>
        /// <returns>Index of particle in "this.ps".</returns>
        int SelectParticle()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            float closestDist = float.MaxValue;
            int closestIndex = -1;
            float radius = ps.radius; // same as Gizmo sphere size

            for (int i = 0; i < ps.maxCount; i++)
            {
                if (!ps.active[i]) continue; // Ignore not active particles

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

        /// <summary>
        /// Creates a drag plane aligned with the selected particle.
        /// </summary>
        /// <param name="selectedIndex">Index of the selected particle.</param>
        /// <returns>Drag plane pointing towards the camera.</returns>
        Plane GetDragPlane(int selectedIndex)
        {
            // Get drag plane
            Vector3 planeNormal = Camera.main.transform.forward;
            Vector3 planePoint = ps.currentPosition[selectedIndex];

            return new(planeNormal, planePoint);
        }

        /// <summary>
        /// Drags particle across drag plane based on mouse movement.
        /// </summary>
        /// <param name="selectedIndex">Index of particle selected to be dragged.</param>
        void DragUpdate(int selectedIndex)
        {
            if (selectedIndex == -1) return;
            if (!ps.active[selectedIndex]) return; // Ignore not active particles

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
            coll.objects.Add(tool);
            coll.objects.Add(canal);

            coll.tool = tool;
            coll.canal = canal;
        }

        /// <summary>
        /// Loop for mouse user input.
        /// </summary>
        private void Update()
        {
            //// Mouse manipulation
            //if (Input.GetMouseButtonUp(0))
            //{
            //    grabbedParticle = -1;
            //    return;
            //}

            //if (grabbedParticle != -1)
            //{
            //    DragUpdate(grabbedParticle);
            //    return;
            //}

            //if (Input.GetMouseButtonDown(0))
            //{
            //    grabbedParticle = SelectParticle();
            //    if (grabbedParticle != -1)
            //    {
            //        dragPlane = GetDragPlane(grabbedParticle);
            //    }
            //}

            //if (Input.GetMouseButtonDown(1))
            //{
            //    selectedParticle = SelectParticle();
            //}
        }

        /// <summary>
        /// Main XPBD simulation loop.
        /// </summary>
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

            if (tool != null)
            {
                tool.previousPosition = tool.transform.position;
                tool.MoveTool(dt);
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

            this.RemoveParticles(ps);

            if (distOn) dist.UpdateRestLengths(ps, dt); // Update rest lengths after solving

            // ------ 6. Update velocities ------
            UpdateVelocities(ps, dt);
            tool.velocity = (tool.transform.position - tool.previousPosition) / dt;

            // ------ 7. Send HapticMessage to NewHapticManager from the collision solver ------
            if (tool != null && tool.keyboardOn) tool.ResetTarget(); // If using keyboard, just reset target
            else hapticManager.SetHapticMessage(coll.GetHapticMessage());
        }

        /// <summary>
        /// Draws particles and constraints for debugging.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (ps == null) return;
            if (ps.currentPosition == null) return;

            // Draw particles
            Gizmos.color = Color.black;
            for (int i = 0; i < ps.maxCount; i++)
            {
                if (!ps.active[i]) continue; // Ignore not active particles

                if (drawParticles) Gizmos.DrawSphere(ps.currentPosition[i], ps.radius * particleViewRadius);

                // Draw adhesion constraints
                if (adhesOn && anchors[i].isActive && drawAdhes)
                {
                    Gizmos.color = Color.green;

                    Vector3 anchorPos = anchors[i].shape.GetWorldPos(anchors[i].localAnchorPos);

                    Gizmos.DrawSphere(anchorPos, ps.radius * particleViewRadius);

                    Gizmos.DrawLine(
                        ps.currentPosition[i],
                        anchorPos);
                    Gizmos.color = Color.black;
                }
            }

            if (drawParticles)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(this.transform.position, this.particleDeleteRadius);
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
