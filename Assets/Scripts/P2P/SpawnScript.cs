using UnityEngine;
using Unity.Netcode;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;
    
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    private NetworkVariable<int> nextSpawnIndex = new NetworkVariable<int>(0);

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
        if (!IsServer || spawnPoints.Length == 0)
            return Vector3.zero; // Fallback (shouldn't happen)

        Vector3 spawnPos = spawnPoints[nextSpawnIndex.Value].position;
        nextSpawnIndex.Value = (nextSpawnIndex.Value + 1) % spawnPoints.Length;
        return spawnPos;
    }
}