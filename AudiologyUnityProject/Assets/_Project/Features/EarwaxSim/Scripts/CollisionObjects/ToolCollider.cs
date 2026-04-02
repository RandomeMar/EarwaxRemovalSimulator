using EarwaxSim;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class ToolCollider : CollisionObjectBase
{
    #region parameters
    public GameObject keyboardInputManager;
    public float toolSpeed = 2f;

    public float rMinor = .2f;
    public Vector3 viewSize;
    public Vector3Int viewResolution;
    [Min(0f)]
    public float viewParticleSize = .1f;
    #endregion


    // Child Colliders
    SphereCollider loopCollider;
    CapsuleCollider shaftCollider;
    
    // Viewer
    ViewingLattice sdfViewer;

    // Input
    PlayerInput playerInput;
    InputAction moveToolAction;


    private void MoveTarget(float dt)
    {
        Vector3 moveDir = moveToolAction.ReadValue<Vector3>();
        this.targetPosition += moveDir * toolSpeed * dt;
    }

    private void MoveTool(float dt)
    {
        this.transform.position = this.targetPosition;
    }

    protected override CollisionShape BuildShapeTree()
    {
        float rloop = loopCollider.radius;
        float rMajor = rloop - rMinor;

        TorusShape loop = new(loopCollider.transform.localPosition, loopCollider.transform.localRotation, rMajor, rMinor);
        CapsuleShape shaft = new(shaftCollider.transform.localPosition, shaftCollider.transform.localRotation, shaftCollider.height, shaftCollider.radius);
        UnionShape union = new(Vector3.zero, Quaternion.identity, loop, shaft);

        return union;
    }


    protected override void Awake()
    {
        loopCollider = GetComponentInChildren<SphereCollider>();
        shaftCollider = GetComponentInChildren<CapsuleCollider>();

        base.Awake(); // Calls BuildShapeTree and BuildMatProps

        sdfViewer = new(viewSize, viewResolution, viewParticleSize);

        playerInput = keyboardInputManager.GetComponent<PlayerInput>();
        moveToolAction = playerInput.actions.FindAction("MoveTool");
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        MoveTarget(dt);
    }

    private void FixedUpdate()
    {
        MoveTool(1f);
    }


    private void OnDrawGizmos()
    {
        sdfViewer.DrawLattice(this);
    }
    
    private void OnValidate()
    {
        this.Awake();
    }

}
