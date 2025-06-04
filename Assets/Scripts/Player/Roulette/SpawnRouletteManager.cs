using UnityEngine;
using Unity.Netcode;

public class SpawnRodinhaManager : NetworkBehaviour
{
    public GameObject RoulettePrefab; // Assign your Rodinha prefab (must have NetworkObject)
    public Transform[] spawnPoints;


    public void InitializeSpawnPoints()
    {
        spawnPoints = new Transform[5];

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            GameObject go = new GameObject("RodinhaSpawnPoint_" + i);
            spawnPoints[i] = go.transform;
        }

        spawnPoints[0].position = new Vector3(668, 20, 1625);
        spawnPoints[1].position = new Vector3(700, 20, 1900);
        spawnPoints[2].position = new Vector3(590, 20, 1800);
        spawnPoints[3].position = new Vector3(520, 20, 1700);
        spawnPoints[4].position = new Vector3(650, 20, 1900);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            InitializeSpawnPoints();
        }
    }


    public float raycastHeightOffset = 2f;
    public LayerMask groundLayer;

    public void SpawnRodinhaAtRandom()
    {
        if (!IsServer) return; // Only server spawns

        int index = Random.Range(0, spawnPoints.Length);
        Vector3 spawnPos = spawnPoints[index].position + Vector3.up * raycastHeightOffset;

        if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 10f, groundLayer))
        {
            Vector3 finalPosition = hit.point;
            Quaternion finalRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            GameObject rodinha = Instantiate(RoulettePrefab, finalPosition, finalRotation);
            rodinha.GetComponent<NetworkObject>().Spawn();

            Debug.Log($"Rodinha spawned at {finalPosition}, aligned to ground.");
        }
        else
        {
            Debug.LogWarning("No ground hit under Rodinha spawn point.");
        }
    }
}
