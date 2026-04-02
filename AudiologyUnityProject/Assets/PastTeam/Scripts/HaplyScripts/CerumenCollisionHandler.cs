using UnityEngine;
using UnityEngine.UI;

public class DespawnTrigger : MonoBehaviour
{
    private int value = 0;
    [SerializeField] Text CerumenText;
    [SerializeField] SpawnSphereScript spawnSphereScript;
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<MovingSphere>() != null)
        {
            value++;
            Destroy(other.gameObject);
            CerumenText.text = $"Cerumen removed: {value}";
            spawnSphereScript.Spawner(1);
        }
    }
}
