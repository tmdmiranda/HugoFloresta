using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

// Recommended settings for P2P in your NetworkManager component
public class NetworkConfigurator : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private UnityTransport transport;
    [SerializeField] private GameObject playerPrefab;

    private void Awake()
    {
        if (networkManager == null) networkManager = GetComponent<NetworkManager>();

        // Basic configuration
        networkManager.NetworkConfig.ProtocolVersion = 1;
        networkManager.NetworkConfig.NetworkTransport = transport;
        networkManager.NetworkConfig.TickRate = 60;
        networkManager.NetworkConfig.SpawnTimeout = 10f;

        // Player prefab assignment
        networkManager.NetworkConfig.PlayerPrefab = playerPrefab;

        // Enable scene management if needed
        networkManager.NetworkConfig.EnableSceneManagement = true;
    }
}