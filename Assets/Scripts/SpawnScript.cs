using UnityEngine;
using Unity.Netcode;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;
    
    [SerializeField] private Transform[] spawnPoints;
    private int nextSpawnIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Vector3 GetNextSpawnPosition()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return Vector3.zero;
        }
        
        Vector3 spawnPos = spawnPoints[nextSpawnIndex].position;
        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;
        return spawnPos;
    }
}