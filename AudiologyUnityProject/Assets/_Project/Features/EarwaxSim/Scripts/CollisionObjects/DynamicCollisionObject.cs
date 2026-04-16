using UnityEngine;
using UnityEngine.InputSystem;

namespace EarwaxSim
{
    public abstract class DynamicCollisionObject : CollisionObjectBase
    {
        [Header("Tool Movement Settings")]
        public GameObject keyboardInputManager;
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

        [System.NonSerialized] public Vector3 collisionForceWorld;


        public virtual void MoveTarget(float dt)
        {
            if (moveToolAction == null) return;
            Vector3 moveDir = moveToolAction.ReadValue<Vector3>();

            //this.targetPosition += moveDir * toolSpeed * dt;

            this.targetPosition = haplyCursor.transform.position;
        }

        //public void ResetTarget()
        //{
        //    this.targetPosition = this.transform.position;
        //}

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

