using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

// Classe responsável por gerar (spawnar) inimigos no cenário
public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab; // Prefab do inimigo a ser instanciado
    public int numberOfEnemies = 5; // Número de inimigos a spawnar
    public float spawnRadius = 20f; // Raio máximo para spawnar inimigos em torno do ponto central
    public float minDistanceBetweenEnemies = 2f; // Distância mínima entre inimigos ao spawnar
    [Tooltip("Layer mask for collision detection (should include enemy layer)")]
    public LayerMask spawnLayerMask = -1; // Máscara de camadas para detetar colisões ao spawnar
    public float navMeshSampleRange = 10f; // Raio para amostragem de posições válidas no NavMesh

    [Header("Debug")]
    public bool enableDebugLogs = true; // Ativa/desativa logs de debug

    // Método chamado quando o objeto é inicializado na rede
    public void OnNetworkSpawn()
    {
        // Apenas o servidor pode spawnar inimigos
        if (!IsServer) return;

        // Verifica se a NavMesh está pronta
        if (!IsNavMeshReady())
        {
            Debug.LogError("NavMesh not ready! Check your NavMesh baking.");
            return;
        }

        // Inicia o spawn dos inimigos
        SpawnEnemies();
    }

    // Verifica se o NavMesh está disponível e pronto para uso
    private bool IsNavMeshReady()
    {
        return NavMesh.CalculateTriangulation().vertices.Length > 0;
    }

    // Método principal para spawnar todos os inimigos
    private void SpawnEnemies()
    {
        int successfullySpawned = 0; // Contador de inimigos spawnados com sucesso
        var navMeshData = NavMesh.CalculateTriangulation(); // Dados do NavMesh

        for (int i = 0; i < numberOfEnemies; i++)
        {
            if (enableDebugLogs) Debug.Log($"=== Attempting to spawn enemy {i} ===");
            Vector3 spawnPos = Vector3.zero;
            bool foundPosition = false;

            // Seleciona aleatoriamente um vértice do NavMesh para tentar spawnar
            int randomIndex = UnityEngine.Random.Range(0, navMeshData.vertices.Length);
            spawnPos = navMeshData.vertices[randomIndex];

            // Verifica colisão com outros inimigos já existentes
            bool hasCollision = Physics.CheckSphere(spawnPos, minDistanceBetweenEnemies, spawnLayerMask);
            if (!hasCollision)
            {
                foundPosition = true;
            }

            if (foundPosition)
            {
                // Spawna o inimigo na posição encontrada
                SpawnSingleEnemy(spawnPos);
                successfullySpawned++;
                if (enableDebugLogs) Debug.Log($"Successfully spawned enemy at {spawnPos}");
            }
            else
            {
                Debug.LogWarning($"FAILED to spawn enemy {i} at position {spawnPos}");
            }
        }

        Debug.Log($"Spawn summary: {successfullySpawned}/{numberOfEnemies} enemies spawned");
    }

    // Spawna um único inimigo numa posição específica
    private void SpawnSingleEnemy(Vector3 position)
    {
        // Garante que a posição está no NavMesh antes de spawnar
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            position = hit.position;
        }
        else
        {
            Debug.LogError($"Failed to find valid NavMesh position for enemy spawn at {position}");
            return;
        }

        // Calcula a direção para o inimigo olhar (do spawner para a posição de spawn)
        Vector3 direction = (position - transform.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);

        // Instancia o inimigo e garante que tem componente de rede
        GameObject enemy = Instantiate(enemyPrefab, position, rotation);
        NetworkObject netObj = enemy.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(); // Torna o inimigo visível para todos os jogadores
            Debug.Log($"Enemy spawned successfully at {position}");
        }
        else
        {
            Debug.LogError("Enemy prefab missing NetworkObject component!");
            Destroy(enemy);
        }
    }

    // Desenha gizmos no editor para visualizar áreas de spawn
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius); // Raio de spawn
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, navMeshSampleRange); // Raio de amostragem do NavMesh
    }
}