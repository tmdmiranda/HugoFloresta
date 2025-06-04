using UnityEngine;
using Unity.Netcode;

public class SpawnRodinhaManager : NetworkBehaviour
{
    public GameObject RoulettePrefab; // Assign your Rodinha prefab (must have NetworkObject)
    private Transform[] spawnPoints;


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
    private float raycastHeightOffset = 2f;
    private LayerMask groundLayer;
}
