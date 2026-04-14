using UnityEngine;
using UnityEngine.InputSystem;

namespace EarwaxSim
{
    public abstract class DynamicCollisionObject : CollisionObjectBase
    {
        [Header("Tool Movement Settings")]
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
        

        // Moves target position based on keyboard input
        public void MoveTarget(float dt)
        {
            Vector3 moveDir = moveToolAction.ReadValue<Vector3>();
            this.targetPosition += moveDir * toolSpeed * dt;
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

            playerInput = keyboardInputManager.GetComponent<PlayerInput>();
            moveToolAction = playerInput.actions.FindAction("MoveTool");
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            MoveTarget(dt);
        }
    }
}

