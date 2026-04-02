using UnityEngine;
using EarwaxSim;

public class RoomObject : CollisionObjectBase
{
    public bool drawRoom;

    private BoxCollider boxCollider;

    protected override CollisionShape BuildShapeTree()
    {
        BoxShape roomArea = new(Vector3.zero, Quaternion.identity, this.boxCollider.size / 2f);
        return new InverseShape(Vector3.zero, Vector3.zero, roomArea);
    }


    protected override void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        base.Awake();
    }

    private void OnValidate()
    {
        this.Awake();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.orange;
        if (drawRoom && boxCollider != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, this.boxCollider.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
