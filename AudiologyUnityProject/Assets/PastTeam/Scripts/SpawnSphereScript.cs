using UnityEngine;

public class SpawnSphereScript : MonoBehaviour
{
    public GameObject spherePrefab;
    public Transform managerObject;
    public int amountToSpawn = 4;
    public float spawn_radius = .05f;

    void Start()
    {
        Spawner(amountToSpawn);
    }

    public void Spawner(int amount)
    {
        float radius = managerObject.localScale.x * 0.5f - 0.001f;


        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPos = new Vector3(0, 0, 0);
            //int random = Random.Range(0, 2);
            //if(random == 1)
            //{
            //    spawnPos = GetRandomEdgePosition(radius);
            //}
            //else
            //{
            //    spawnPos = GetRandomEdgePosition(radius);

            //}

            spawnPos = GetRandomEdgePosition(radius);
            //Vector3 spawnPos = GetRandomPosition(radius);
            GameObject new_sphere = Instantiate(spherePrefab, spawnPos, Quaternion.identity);
            new_sphere.transform.localScale = new Vector3(spawn_radius, spawn_radius, spawn_radius);
        }
    }

    //Vector3 GetRandomBottomEdgePosition(float radius)
    //{
    //    float height = managerObject.localScale.y;
    //    float angle = Random.Range(0f, 2f * Mathf.PI);
    //    float x = Mathf.Cos(angle) * radius;
    //    //float y = Mathf.Sin(angle) * radius;
    //    float y = Random.Range(-height / 2, -height / 4);
    //    //float z = Mathf.Sin(angle) * radius;
    //    float z = Random.Range((float)(-managerObject.localScale.z + 0.2), (float)(managerObject.localScale.z - 0.2));
    //    Vector3 localPos = new Vector3(x, y, z);

    //    return managerObject.position + localPos;
    //}

    //Vector3 GetRandomTopEdgePosition(float radius)
    //{
    //    float height = managerObject.localScale.y;
    //    float angle = Random.Range(0f, 2f * Mathf.PI);
    //    float x = Mathf.Cos(angle) * radius;
    //    //float y = Mathf.Sin(angle) * radius;
    //    float y = Random.Range(height / 4, height/2);
    //    //float z = Mathf.Sin(angle) * radius;
    //    float z = Random.Range((float)(-managerObject.localScale.z + 0.2), (float)(managerObject.localScale.z - 0.2));
    //    Vector3 localPos = new Vector3(x, y, z);

    //    return managerObject.position + localPos;
    //}

    Vector3 GetRandomEdgePosition(float radius)
    {
        float height = managerObject.localScale.y;
        float angle = Random.Range(0f, 2f * Mathf.PI);
        float x = Mathf.Cos(angle) * radius;
        float y = Mathf.Sin(angle) * radius;
        //float y = Random.Range(height / 4, height / 2);
        //float z = Mathf.Sin(angle) * radius;
        float z = Random.Range((float)(-managerObject.localScale.z + 0.2), (float)(managerObject.localScale.z - 0.2));
        Vector3 localPos = new Vector3(x, y, z);

        return managerObject.position + localPos;
    }

    //Vector3 GetRandomPosition(float radius)
    //{
    //    float height = managerObject.localScale.y;

    //    float angle = Random.Range(0f, Mathf.PI * 2);


    //    float r = radius * Mathf.Sqrt(Random.value);
    //    float x = r * Mathf.Cos(angle);

    //    float z = r * Mathf.Sin(angle);
    //    float y = Random.Range(-height, height);

    //    Vector3 localPos = new Vector3(x, y, z);

    //    return managerObject.TransformPoint(localPos);
    //}
}