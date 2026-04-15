using UnityEngine;
using UnityEngine.InputSystem;

namespace EarwaxSim
{
    public abstract class DynamicCollisionObject : CollisionObjectBase
    {
        [Header("Tool Movement Settings")]
        public bool useHapticInput = false; //tick on if this tool should be driven by a Haply device via HapticManager. Leave off to use the fallbacks below.
        public Transform followTransform; //optional fallback. If assigned, tool will chase this transform's position. If null, falls back to keyboard input (if keyboardInputManager is assigned) or no movement at all.
        public GameObject keyboardInputManager; 

        public float toolSpeed = 2f;
        public float maxSpeed = 10f;

        [Header("Viewer Settings")]
        public Vector3 viewSize;
        public Vector3Int viewResolution;
        [Min(0f)]
        public float viewParticleSize = .1f;
        [Min(0f)]
        public float viewCutoff = .1f;

        // Input
        protected PlayerInput playerInput;
        protected InputAction moveToolAction;

        // Force feedback handoff. Written by XPBDSim.FixedUpdate on the main thread
        [System.NonSerialized] public Vector3 collisionForceWorld;
        

        // Moves target position based on keyboard input. if no keyboard input manager is assigned
        public void MoveTarget(float dt)
        {
            if (moveToolAction == null) return;
            Vector3 moveDir = moveToolAction.ReadValue<Vector3>();
            this.targetPosition += moveDir * toolSpeed * dt;
        }

        //Called from HapticManager on the haptic thread
        public virtual void MoveTarget(Vector3 pose)
        {
            this.targetPosition = pose;
        }

        public void ResetTarget()
        {
            this.targetPosition = this.transform.position;
        }

        // Moves tool position based on target position
        public void MoveTool(float dt)
        {
            Vector3 delta = this.targetPosition - this.transform.position;
            float dist = delta.magnitude;

            if (dist <= Constants.EPS) return;

            float maxStep = maxSpeed * dt;
            float step = Mathf.Min(dist, maxStep);

            this.transform.position += delta / dist * step;
        }


        protected override void Awake()
        {
            base.Awake(); // Calls BuildShapeTree and BuildMatProps

            // Keyboard input is optional if no manager is assigned, HaplyToolDriver is expected to write targetPosition.
            if (keyboardInputManager != null)
            {
                playerInput = keyboardInputManager.GetComponent<PlayerInput>();
                if (playerInput != null)
                    moveToolAction = playerInput.actions.FindAction("MoveTool");
            }
        }

        private void Update()
        {
            if (useHapticInput) return;

            if (followTransform != null)
            {
                this.targetPosition = followTransform.position;
                return;
            }
            if (moveToolAction == null) return;
            float dt = Time.deltaTime;
            MoveTarget(dt);
        }
    }
}

