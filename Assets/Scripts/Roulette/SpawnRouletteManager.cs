using UnityEngine;
using Unity.Netcode;

public class SpawnRodinhaManager : NetworkBehaviour
{
    public GameObject RoulettePrefab; // Must have a NetworkObject

    [SerializeField] private LayerMask groundLayer;
    private GameObject currentRoulette;

    public void SpawnRoulette()
    {
        if (!IsServer) return;

        Debug.Log("Spawning roulette...");

        if (RoulettePrefab == null)
        {
            Debug.LogError("Roulette prefab not assigned!");
            return;
        }

        // Choose a random spawn position from your predefined points


        Vector3 initialSpawnPosition = GetRandomSpawnPosition();
        Vector3 finalSpawnPoint = initialSpawnPosition;

        // Start high and raycast down to find ground
        RaycastHit hit;
        if (Physics.Raycast(initialSpawnPosition, Vector3.down, out hit, 500f)) // adicionei um quito +dist
        {
            finalSpawnPoint = hit.point + (Vector3.up * 0.1f);
            Debug.Log($"hit:{hit.point} final:{finalSpawnPoint}");
        }

        // Destroy existing roulette first
        DestroyCurrentRoulette();

        // Create and spawn the new roulette
        GameObject roleta = Instantiate(RoulettePrefab, finalSpawnPoint, Quaternion.identity);
        NetworkObject roletaNetObj = roleta.GetComponent<NetworkObject>();

        if (roletaNetObj == null)
        {
            Debug.LogError("Roulette prefab is missing NetworkObject component!");
            Destroy(roleta);
            return;
        }

        roletaNetObj.Spawn();
        currentRoulette = roleta;
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

    public void DestroyCurrentRoulette()
    {
        // Find by tag instead of name
        currentRoulette = GameObject.FindWithTag("Roulette"); // Make sure your roulette prefab has this tag

        Debug.Log("Destroying current Rodinha...");
        if (currentRoulette != null)
        {
            if (currentRoulette.TryGetComponent(out NetworkObject netObj))
            {
                if (netObj.IsSpawned)
                    netObj.Despawn();
            }
            Destroy(currentRoulette);
        }
    }
}