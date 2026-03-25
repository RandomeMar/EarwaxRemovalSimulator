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

        [Header("Adhesion Constraint Settings")]
        public bool adhesOn;
        [Min(0f)]
        public float adhesCompliance;
        [Min(0f)]
        public float adhesBreakDist;

        [Header("Distance Constraint Settings")]
        public bool distOn = true;
        public float yieldStrain = .5f;
        public float plasticFlow = 1f;
        public float breakStrain = 1f;

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

        [Header("Torus Tool Settings")]
        public Vector3 torPosition;
        public float rMajor;
        public float rMinor;

        [Header("Viewing Slice Settings")]
        public Vector3 viewerPosition = Vector3.zero;
        public Vector2 viewerSize;
        public Vector2Int viewerResolution;
        public float viewerParticleSize;
        #endregion

        #region Private Input Values
        private PlayerInput playerInput;
        private InputAction moveToolAction;
        private Vector3 moveDir;

        private BoxShape roomArea;
        private SphereShape toolShape;
        private TorusShape torusTool;

        private ViewingSlice viewer;
        #endregion

        #region Solver Objects
        ParticleSet ps;
        SpatialHash grid;
        AdhesionAnchor[] anchors;

        // Solvers
        DistanceConstraintSet dist;
        DensityConstraintSolver dense;
        CollisionConstraintSolver coll;
        AdhesionConstraintSolver adhes;
        #endregion


        // ------ Functions ------

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
                    for (int k = 0; k < n; k++)
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

            DistanceConstraintSet dcs = new(dist.ToArray(), yieldStrain, plasticFlow, breakStrain, adaptRate, recoveryRate);

            SpatialHash grid = new SpatialHash(h, particleCount);

            grid.BuildGrid(lattice);

            float restDensity = CalcRestDensity(lattice, grid, h);

            DensityConstraintSolver dense = new(restDensity, h, denseCompliance);

            print("GENERATED LATTICE!!!");

            return (lattice, grid, dcs, dense);
        }

        void CreateBasicRoom(CollisionConstraintSolver coll)
        {
            roomArea = new(new Vector3(0f, roomDimensions.y, 0f), roomRotation, roomDimensions);
            InverseShape room = new(roomArea, Vector3.zero, Vector3.zero);
            CollisionObject roomObj = new(room, dynamicFriction);
            coll.objects.Add(roomObj);

            //SphereShape s1 = new(new Vector3(-roomDimensions.x / 2, roomDimensions.y, 0f), roomDimensions.y);
            //SphereShape s2 = new(new Vector3(roomDimensions.x / 2, roomDimensions.y, 0f), roomDimensions.y);
            //DifferenceShape diff = new(s1, s2);
            //InverseShape room = new(diff);
            //CollisionObject roomObj = new(room, dynamicFriction);
            //coll.objects.Add(roomObj);

        }

        void AddTool(CollisionConstraintSolver coll, Vector3 center, float radius)
        {
            toolShape = new(center, Vector3.zero, radius);
            CollisionObject toolObj = new(toolShape, dynamicFriction);
            coll.objects.Add(toolObj);
        }

        void AddTorusTool(CollisionConstraintSolver coll, Vector3 position, float rMajor, float rMinor, float friction)
        {
            torusTool = new(position, Vector3.zero, rMajor, rMinor);
            CollisionObject toolObj = new(torusTool, friction);
            coll.objects.Add(toolObj);
        }

        // Creates lattice, room, and sphere tool
        void BuildSimulation()
        {
            (ps, grid, dist, dense) = GenerateLattice();

            anchors = new AdhesionAnchor[ps.count];

            adhes = new AdhesionConstraintSolver(adhesCompliance, adhesBreakDist);
            adhes.anchors = anchors;

            coll = new CollisionConstraintSolver(collCompliance, anchors);

            //AddTool(coll, toolSpawn, toolRadius);
            AddTorusTool(coll, torPosition, rMajor, rMinor, dynamicFriction);

            CreateBasicRoom(coll); // Make sure the room is the last collision shape added to coll. The last added collision shape has priority over the others

            viewer = new(viewerPosition, Quaternion.identity, viewerSize, viewerResolution, viewerParticleSize); // Creates view plane for seeing weird collision shapes
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

        Plane GetDragPlane(int selectedIndex)
        {
            // Get drag plane
            Vector3 planeNormal = Camera.main.transform.forward;
            Vector3 planePoint = ps.currentPosition[selectedIndex];

            return new(planeNormal, planePoint);
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

            // Move ball tool
            if (toolShape != null) toolShape.position += moveDir * toolSpeed * dt;
            if (torusTool != null) torusTool.position += moveDir * toolSpeed * dt;


            // 1. Reset lambda
            if (distOn) dist.ResetLambda();
            if (denseOn) dense.ResetLambda();
            if (adhesOn) adhes.ResetLambda();

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
                if (adhesOn) adhes.SolveOnce(ps, dt, grid);
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
                Gizmos.color = Color.black;
                for (int i = 0; i < ps.currentPosition.Length; i++)
                {
                    Gizmos.DrawSphere(ps.currentPosition[i], .08f);

                    if (adhesOn && anchors[i].isActive)
                    {
                        Gizmos.color = Color.green;

                        Vector3 anchorPos = anchors[i].owner.GetWorldPos(anchors[i].localPos);

                        Gizmos.DrawSphere(anchorPos, .05f);

                        Gizmos.DrawLine(
                            ps.currentPosition[i],
                            anchorPos);
                        Gizmos.color = Color.black;
                    }
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
                Gizmos.matrix = Matrix4x4.TRS(roomArea.position, roomArea.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, roomDimensions * 2);
                Gizmos.matrix = Matrix4x4.identity;
            }

            //// TEMP Draw ball room
            //Gizmos.DrawWireSphere(new Vector3(-roomDimensions.x / 2, roomDimensions.y, 0f), roomDimensions.y);
            //Gizmos.DrawWireSphere(new Vector3(roomDimensions.x / 2, roomDimensions.y, 0f), roomDimensions.y);

            if (toolShape != null) viewer.DrawSlice(toolShape);
            if (torusTool != null) viewer.DrawSlice(torusTool);

            // Draw colliders
            foreach (CollisionObject obj in coll.objects)
            {
                CollisionShape shape = obj.shape;

                if (shape is SphereShape sphere) Gizmos.DrawWireSphere(sphere.position, sphere.radius);
                else if (shape is PlaneShape plane) Gizmos.DrawSphere(plane.position, .1f);
                else if (shape is CapsuleShape capsule)
                {
                    Gizmos.DrawWireSphere(capsule.a, capsule.radius);
                    Gizmos.DrawWireSphere(capsule.b, capsule.radius);
                }
                else if (shape is BoxShape box) Gizmos.DrawWireCube(box.position, box.b * 2f);
                else if (shape is TorusShape torus) Gizmos.DrawWireSphere(torus.position, torus.rMajor);
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
}
