using UnityEngine;

[ExecuteAlways] // This script is always running inside the editor
public class CanalNode : MonoBehaviour
{
    [Min(0)]
    public float rx;
    [Min(0)]
    public float rz;

    private EarCollisionObject earColl;

    private void OnEnable()
    {
        earColl = GetComponentInParent<EarCollisionObject>();
        NotifyParent();
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            transform.hasChanged = false;
            NotifyParent();
        }
    }

    private void OnValidate()
    {
        NotifyParent();
    }

    private void NotifyParent()
    {
        if (earColl == null)
            earColl = GetComponentInParent<EarCollisionObject>();

        if (earColl != null)
            earColl.Rebuild();
    }
}