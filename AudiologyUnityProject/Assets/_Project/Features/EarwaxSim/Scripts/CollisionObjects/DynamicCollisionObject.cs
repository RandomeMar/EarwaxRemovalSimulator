using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR.Haptics;

namespace EarwaxSim
{
    /// <summary>
    /// Collision object that moves through user input.
    /// </summary>
    public abstract class DynamicCollisionObject : CollisionObjectBase
    {
        [Header("Tool Movement Settings")]
        public GameObject keyboardInputManager;
        public Camera cam;
        public GameObject haplyCursor;

        public bool keyboardOn = false;

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


        /// <summary>
        /// Update target position and rotation using keyboard or haply cursor.
        /// </summary>
        /// <param name="dt">Delta time</param>
        /// <remarks>If the bool "keyboardOn" is true, keyboard input will move the target.</remarks>
        public virtual void MoveTarget(float dt)
        {
            if (moveToolAction == null) return;
            Vector3 moveDir = moveToolAction.ReadValue<Vector3>();

            // Keyboard controller
            if (keyboardOn)
            {
                this.targetPosition += cam.transform.TransformDirection(moveDir) * toolSpeed * dt;
                return;
            }
            
            // Haptic controller
            this.targetPosition = haplyCursor.transform.position;
            this.targetRotation = haplyCursor.transform.rotation;
        }

        /// <summary>
        /// Resets target position to the curette's position.
        /// </summary>
        /// <remarks>This method should only be called if the user is using keyboard input.</remarks>
        public void ResetTarget()
        {
            this.targetPosition = this.transform.position;
        }

        /// <summary>
        /// Moves tool position based on target position.
        /// </summary>
        /// <param name="dt">Delta time.</param>
        public void MoveTool(float dt)
        {
            Vector3 delta = this.targetPosition - this.transform.position;
            float dist = delta.magnitude;

            if (dist <= Constants.EPS) return;

            float maxStep = maxSpeed * dt;
            float step = Mathf.Min(dist, maxStep);

            this.transform.position += delta / dist * step;

            this.transform.rotation = this.targetRotation;
        }


        protected override void Awake()
        {
            base.Awake();

            if (keyboardInputManager != null)
            {
                playerInput = keyboardInputManager.GetComponent<PlayerInput>();
                if (playerInput != null)
                    moveToolAction = playerInput.actions.FindAction("MoveTool");
            }
        }

        protected virtual void Update()
        {
            if (moveToolAction == null) return;
            float dt = Time.deltaTime;
            MoveTarget(dt);
        }
    }
}

