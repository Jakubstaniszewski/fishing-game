using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    public GameObject prefab;
    public float spawnInterval = 13f;
    public float rangeX = 5f;
    public float rangeZ = 5f;
    public float maxFish = 33f;
    public float fishSpawned = 0f;
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnPrefab();
            timer = 0f;
        }
    }

    void SpawnPrefab()
    {

        if (fishSpawned >= maxFish)
        {
            return;
        }
        if (prefab == null)
        {
            Debug.LogWarning("No prefab assigned to PrefabSpawner");
            return;
        }

        float randomX = Random.Range(-rangeX, rangeX);
        float randomZ = Random.Range(-rangeZ, rangeZ);

        Vector3 spawnPosition = new Vector3(        
            transform.position.x + randomX,
            transform.position.y,
            transform.position.z + randomZ
        );

        GameObject spawned = Instantiate(prefab, spawnPosition, prefab.transform.rotation);
        fishSpawned++;
    }
}