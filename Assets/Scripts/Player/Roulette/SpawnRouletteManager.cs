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
        Vector3 spawnPoint = GetRandomSpawnPosition();

        // Start high and raycast down to find ground
        Vector3 rayStart = spawnPoint + Vector3.up * 100f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 200f, groundLayer))
        {
            spawnPoint = hit.point + Vector3.up * 0.1f; // Slightly above ground
        }

        // Destroy existing roulette first
        DestroyCurrentRoulette();

        // Create and spawn the new roulette
        GameObject roleta = Instantiate(RoulettePrefab, spawnPoint, Quaternion.identity);
        NetworkObject roletaNetObj = roleta.GetComponent<NetworkObject>();

        if (roletaNetObj == null)
        {
            Debug.LogError("Roulette prefab is missing NetworkObject component!");
            Destroy(roleta);
            return;
        }

        roletaNetObj.Spawn();
        currentRoulette = roleta;
        Debug.Log("Roulette spawned successfully at: " + spawnPoint);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // Your predefined spawn points
        Vector3[] spawnPoints = new Vector3[]
        {
            new Vector3(668, 20, 1625),
            new Vector3(700, 20, 1845),
            new Vector3(590, 20, 1800),
            new Vector3(520, 20, 1700),
            new Vector3(650, 20, 1845)
        };

        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    public void DestroyCurrentRoulette()
    {
        currentRoulette = GameObject.Find("Roleta(Clone)");

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