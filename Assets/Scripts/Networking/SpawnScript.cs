using UnityEngine;
using Unity.Netcode;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Server-authoritative spawn position selection
    public Vector3 GetNextSpawnPosition()
    {
        if (!IsServer)
            return Vector3.zero; // Fallback (shouldn't happen)

        return GetRandomSpawnPosition();
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // Your predefined spawn points
        Vector3[] spawnPoints = new Vector3[]
        {
            new Vector3(474, 100, 612),
            new Vector3(658, 100, 289),
            new Vector3(608, 100, 289),
            new Vector3(550, 100, 289),
            new Vector3(500, 100, 289),
            new Vector3(480, 100, 331),
        };

        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}