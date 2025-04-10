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
    public LayerMask spawnLayerMask;
    public float navMeshSampleRange = 10f;

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
            Debug.Log($"=== Attempting to spawn enemy {i} ===");
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
                Debug.Log($"Successfully spawned enemy at {spawnPos}");
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
        Debug.Log($"Attempt {attemptNumber}: Trying point {randomPoint}");

        // Find nearest NavMesh position
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navMeshSampleRange, NavMesh.AllAreas))
        {
            Debug.Log($"Found NavMesh position at {hit.position} (distance: {hit.distance})");

            // Check for collisions
            /*bool hasCollision = Physics.CheckSphere(hit.position, minDistanceBetweenEnemies, spawnLayerMask);
            Debug.Log($"Collision check: {(hasCollision ? "FAILED" : "PASSED")}");*/

            //if (!hasCollision)
            //{
                return hit.position;
            //}
        }
        else
        {
            Debug.Log("No NavMesh found near this point");
        }
        
        return Vector3.zero;
    }

    private void SpawnSingleEnemy(Vector3 position)
    {
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        NetworkObject netObj = enemy.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
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