using UnityEngine;

public class ToolCleaner : MonoBehaviour
{
    // Tag of the main tool root (or any part of the tool)
    public string toolTag = "PlayerTool"; 

    private void OnTriggerEnter(Collider other)
    {
        // 1. Check if the object is part of the tool
        // Checking 'root' ensures we catch it even if the trigger hits a tiny screw on the tool
        if (other.CompareTag(toolTag) || other.transform.root.CompareTag(toolTag))
        {
            CleanTheTool(other.transform.root);
        }
    }

    private void CleanTheTool(Transform toolRoot)
    {
        // 2. Scan the ENTIRE hierarchy (Deep Search)
        // We look for the script "EarWaxGunkScript" attached to any child object.
        // 'true' inside the parentheses means "include inactive objects too" just in case.
        EarWaxGunkScript[] allWax = toolRoot.GetComponentsInChildren<EarWaxGunkScript>(true);

        // 3. Destroy them
        foreach (EarWaxGunkScript wax in allWax)
        {
            Destroy(wax.gameObject);
        }

        // Optional Debug
        if (allWax.Length > 0)
        {
            // Debug.Log($"Cleaned {allWax.Length} cubes from the tool.");
        }
    }
}