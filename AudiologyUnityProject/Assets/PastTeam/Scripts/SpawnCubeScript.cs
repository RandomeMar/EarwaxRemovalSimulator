using UnityEngine;

public class SpawnCubeScript : MonoBehaviour
{
    public GameObject cubePrefab;
    
    // NEW: Explicitly define where the center of the tunnel is
    // If you leave this empty, it defaults to the script's own position
    public Transform tunnelCenter; 

    public float tunnelRadius = 2.0f;
    public LayerMask wallLayer;

    public float gap = 0.0f;
    public float manualObjectSize = 0f;

    void Start()
    {
        // Fallback: If no specific center is assigned, use this object's transform
        if (tunnelCenter == null) tunnelCenter = transform;

        SpawnGrid();
    }

    void SpawnGrid()
    {
        float cubeSize = (manualObjectSize > 0) ? manualObjectSize : cubePrefab.transform.localScale.x;
        float stepSize = cubeSize + gap;
        int steps = Mathf.CeilToInt(tunnelRadius / stepSize);

        for (int x = -steps; x <= steps; x++)
        {
            for (int y = -steps; y <= steps; y++)
            {
                Vector3 localSpawnPos = new Vector3(x * stepSize, 0, y * stepSize);

                // CHANGED: Use tunnelCenter instead of transform
                Vector3 worldSpawnPos = tunnelCenter.TransformPoint(localSpawnPos);

                if (CanSpawnHere(worldSpawnPos, cubeSize))
                {
                    // CHANGED: Rotation matches the tunnelCenter
                    // Parent is set to tunnelCenter to keep the hierarchy clean
                    Instantiate(cubePrefab, worldSpawnPos, tunnelCenter.rotation, tunnelCenter);
                }
            }
        }
    }

    bool CanSpawnHere(Vector3 pos, float size)
    {
        Vector3 halfExtents = Vector3.one * (size / 2f);
        
        // CHANGED: Use tunnelCenter.rotation for the box check orientation
        bool hitWall = Physics.CheckBox(pos, halfExtents, tunnelCenter.rotation, wallLayer);

        if (hitWall) return false;

        // CHANGED: Check distance from tunnelCenter
        if (Vector3.Distance(tunnelCenter.position, pos) > tunnelRadius) return false;

        return true;
    }

    void OnDrawGizmosSelected()
    {
        // Handle visualization if tunnelCenter isn't assigned yet (e.g. in Editor)
        Transform reference = tunnelCenter != null ? tunnelCenter : transform;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(reference.position, tunnelRadius);
    }
}