using EarwaxSim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class TorusToolObject : CollisionObjectBase
{
    [Header("Tool Movement Settings")]
    public GameObject keyboardInputManager;
    public float toolSpeed = 2f;

    [Header("Torus Dimensions")]
    public float rMajor;
    public float rMinor;

    [Header("Viewer Settings")]
    public Vector3 viewSize;
    public Vector3Int viewResolution;
    [Min(0f)]
    public float viewParticleSize = .1f;


    // Input
    PlayerInput playerInput;
    InputAction moveToolAction;

    public void MoveTarget(float dt)
    {
        Vector3 moveDir = moveToolAction.ReadValue<Vector3>();
        this.targetPosition = this.transform.position + moveDir * toolSpeed * dt;
    }

    public void MoveTool(float dt)
    {
        this.transform.position = this.targetPosition;
    }


    protected override CollisionShape BuildShapeTree()
    {
        return new TorusShape(Vector3.zero, Quaternion.identity, rMajor, rMinor);
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

    private void OnValidate()
    {
        this.Awake();
    }
}
