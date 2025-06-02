using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public int numberOfEnemies = 5;
    public float spawnRadius = 20f;
    public float minDistanceBetweenEnemies = 2f;
    [Tooltip("Layer mask for collision detection (should include enemy layer)")]
    public LayerMask spawnLayerMask = -1; // Default to everything
    public float navMeshSampleRange = 10f;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        
        if (!IsNavMeshReady())  
        {
            Debug.LogError("NavMesh not ready! Check your NavMesh baking.");
            return;
        }
        
    SpawnEnemies();
    }

    private bool IsNavMeshReady()
    {
        return NavMesh.CalculateTriangulation().vertices.Length > 0;
    }

    private void SpawnEnemies()
    {
        int successfullySpawned = 0;
        
        for (int i = 0; i < numberOfEnemies; i++)
        {
            if (enableDebugLogs) Debug.Log($"=== Attempting to spawn enemy {i} ===");
            Vector3 spawnPos = Vector3.zero;
            bool foundPosition = false;
            
            for (int attempt = 0; attempt < 50; attempt++)
            {
                spawnPos = FindValidSpawnPosition(attempt);
                if (spawnPos != Vector3.zero)
                {
                    foundPosition = true;
                    break;
                }
            }

            if (foundPosition)
            {
                SpawnSingleEnemy(spawnPos);
                successfullySpawned++;
                if (enableDebugLogs) Debug.Log($"Successfully spawned enemy at {spawnPos}");
            }
            else
            {
                Debug.LogWarning($"FAILED to spawn enemy {i} after 50 attempts");
            }
        }
        
        Debug.Log($"Spawn summary: {successfullySpawned}/{numberOfEnemies} enemies spawned");
    }

    private Vector3 FindValidSpawnPosition(int attemptNumber)
    {
        // Generate random point in circle
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        if (enableDebugLogs) if (enableDebugLogs) Debug.Log($"Attempt {attemptNumber}: Trying point {randomPoint}");

        // Find nearest NavMesh position
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navMeshSampleRange, NavMesh.AllAreas))
        {
            if (enableDebugLogs) if (enableDebugLogs) Debug.Log($"Found NavMesh position at {hit.position} (distance: {hit.distance})");

            // Check for collisions with existing enemies
            bool hasCollision = Physics.CheckSphere(hit.position, minDistanceBetweenEnemies, spawnLayerMask);
            if (enableDebugLogs) if (enableDebugLogs) Debug.Log($"Collision check: {(hasCollision ? "FAILED" : "PASSED")}");

          if (!hasCollision)
          {
                return hit.position;
          }
        }
        else
        {
            if (enableDebugLogs) if (enableDebugLogs) Debug.Log("No NavMesh found near this point");
        }
        
        return Vector3.zero;
    }

    private void SpawnSingleEnemy(Vector3 position)
    {
        // Ensure the position is on the NavMesh before spawning
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            position = hit.position;
        }
        else
        {
            Debug.LogError($"Failed to find valid NavMesh position for enemy spawn at {position}");
            return;
        }

        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        NetworkObject netObj = enemy.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
            Debug.Log($"Enemy spawned successfully at {position}");
        }
        else
        {
            Debug.LogError("Enemy prefab missing NetworkObject component!");
            Destroy(enemy);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, navMeshSampleRange);
    }
}