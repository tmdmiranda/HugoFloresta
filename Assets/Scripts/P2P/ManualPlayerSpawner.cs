using Unity.Netcode;
using UnityEngine;

public class ManualPlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private void Start()
    {
        // Disable all automatic spawning
        ConfigureNetworkManager();
        
        // Register connection callbacks
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void ConfigureNetworkManager()
    {
        var config = NetworkManager.Singleton.NetworkConfig;
        
        config.PlayerPrefab = null; // Clear any assigned prefab
        
        // Optional: If using scene management
        config.EnableSceneManagement = false;
    }

    private void OnServerStarted()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            SpawnPlayer(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
        {
            SpawnPlayer(clientId);
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        int spawnIndex = (int)clientId % spawnPoints.Length;
        GameObject player = Instantiate(
            playerPrefab,
            spawnPoints[spawnIndex].position,
            spawnPoints[spawnIndex].rotation
        );

        var netObj = player.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);
        
        Debug.Log($"Manually spawned player for client {clientId}");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}