using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Users;
using System.Runtime.InteropServices;

using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using System.Net.NetworkInformation;



public class P2P_Manager : NetworkBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField nameInputField;
    public TMP_InputField ipInputField;
    public TMP_Text hostIp;
    public ushort port = 25000;
    public TMP_Text connectionStatusText;

    private bool isPlayerPrefabRegistered = false;
    public int MaxConnections = 8;
    public GameObject LobbyPanelPrefab;
    public NetworkPrefab PlayerPrefab;

    private UnityTransport transport;
    private NetworkList<PlayerLobbyData> playerData;
    private GameObject lobbyPanelInstance;
    [SerializeField] private EnemySpawner enemySpawner;

    public static P2P_Manager Instance { get; private set; }

    public ulong LocalClientId { get; private set; }
    public NetworkObject LocalPlayerObject { get; private set; }

    private readonly Dictionary<ulong, NetworkObject> playerObjects = new Dictionary<ulong, NetworkObject>();



    public override void OnNetworkSpawn()
    {
        if (IsClient && IsOwner)
        {
            LocalClientId = NetworkManager.Singleton.LocalClientId;
        }


        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        CreateLobbyUI();
        playerData.OnListChanged += OnPlayerListChanged;
    }





    public NetworkObject GetPlayerObject(ulong clientId)
    {
        playerObjects.TryGetValue(clientId, out var obj);
        return obj;
    }

    public bool IsLocalPlayer(NetworkObject obj)
    {
        return obj != null && obj == LocalPlayerObject;
    }
    [SerializeField] GameObject vanPrefab;

    public struct PlayerLobbyData : INetworkSerializable, IEquatable<PlayerLobbyData>
    {
        public ulong clientId;
        public FixedString32Bytes playerName;
        public bool isReady;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref playerName);
            serializer.SerializeValue(ref isReady);
        }

        public bool Equals(PlayerLobbyData other) => clientId == other.clientId;
        public override bool Equals(object obj) => obj is PlayerLobbyData other && Equals(other);
        public override int GetHashCode() => clientId.GetHashCode();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        playerData = new NetworkList<PlayerLobbyData>();
    }


    private void Start()
    {
        StartCoroutine(InitializeNetwork());
    }

    private void RegisterPlayerPrefab()
    {
        if (PlayerPrefab.Prefab == null)
        {
            Debug.LogError("PlayerPrefab is not assigned!");
            return;
        }

        var netObj = PlayerPrefab.Prefab.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("PlayerPrefab is missing NetworkObject component!");
            return;
        }

        var prefabList = NetworkManager.Singleton.NetworkConfig.Prefabs;

        bool alreadyRegistered = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs.Any(p => p.Prefab == PlayerPrefab.Prefab);


        if (!alreadyRegistered)
        {
            prefabList.Add(PlayerPrefab);
            Debug.Log("Player prefab registered successfully");
        }
        else
        {
            Debug.Log("Player prefab already registered");
        }

        isPlayerPrefabRegistered = true;
    }

    private IEnumerator InitializeNetwork()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);
        NetworkManager.Singleton.NetworkConfig.PlayerPrefab = null;

        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
            transport = NetworkManager.Singleton.gameObject.AddComponent<UnityTransport>();

        transport.SetConnectionData("0.0.0.0", port);

        RegisterPlayerPrefab();

    }
    public void StartGame()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
        StartCoroutine(DelayedSpawnPlayers());
    }

    private IEnumerator DelayedSpawnPlayers()
    {
        Debug.Log("Waiting before spawning players...");
        yield return new WaitForSeconds(2f); // wait 2 seconds for the scene to finish loading

        Debug.Log("Spawning players now...");
        StartCoroutine(SpawnPlayersOneByOne());
    }


    // Add this to your P2P_Manager class
    private string GetPlayerName(ulong clientId)
    {
        // First check lobby data
        foreach (var player in playerData)
        {
            if (player.clientId == clientId)
            {
                return player.playerName.ToString();
            }
        }

        // Then check PlayerDataManager if available
        if (PlayerDataManager.Instance != null &&
            PlayerDataManager.Instance.TryGetPlayerName(clientId, out string name))
        {
            return name;
        }

        // Final fallback
        return $"Player{clientId}";
    }

    private IEnumerator SpawnPlayersOneByOne()
    {
        Debug.Log("Starting player spawn sequence...");
        if (!IsServer)
        {
            Debug.Log("Not server, aborting spawn sequence");
            yield break;
        }

        RegisterPlayerPrefab();
        if (!isPlayerPrefabRegistered)
        {
            Debug.LogError("Cannot spawn players - prefab not registered!");
            yield break;
        }

        yield return null;

        var clients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        clients.Sort();

        Debug.Log($"Will spawn {clients.Count} players");

        for (int i = 0; i < clients.Count; i++)
        {
            ulong clientId = clients[i];
            Debug.Log($"Processing spawn for client {clientId} ({i + 1}/{clients.Count})");

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
                client.PlayerObject != null)
            {
                Debug.Log($"Player already exists for client {clientId}, skipping spawn");
                continue;
            }

            Vector3 spawnPos = CalculateSpawnPosition(i, clients.Count);
            GameObject player = Instantiate(PlayerPrefab.Prefab, spawnPos, Quaternion.identity);
            NetworkObject netObj = player.GetComponent<NetworkObject>();

            if (netObj == null)
            {
                Debug.LogError($"PlayerPrefab is missing NetworkObject component for client {clientId}");
                continue;
            }

            netObj.SpawnAsPlayerObject(clientId);


            // Set player name
            string playerName = GetPlayerName(clientId);
            var uniquePlayer = player.GetComponent<UniquePlayer>();
            if (uniquePlayer != null)
            {
                uniquePlayer.SetPlayerName(playerName);
            }
            else
            {
                Debug.LogError($"UniquePlayer component missing on player prefab for client {clientId}");
            }

            Debug.Log($"Successfully spawned player '{playerName}' for client {clientId} at {spawnPos}");
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("Finished spawning all players");
        SpawnVan();
        enemySpawner.OnNetworkSpawn();
    }

    private Vector3 CalculateSpawnPosition(int index, int totalPlayers)
    {
        float radius = 5f;
        float angle = index * (2f * Mathf.PI / totalPlayers);
        Vector3 center = new Vector3(915f, 50f, 418f);

        return center + new Vector3(
            Mathf.Cos(angle) * radius,
            0f,
            Mathf.Sin(angle) * radius
        );
    }

    public void SpawnVan()
    {
        if (!IsServer) return;

        if (vanPrefab == null)
        {
            Debug.LogError("Van prefab not found in Resources!");
            return;
        }

        Vector3 spawnPoint = new Vector3(915f, 100f, 423f); // start high
        if (Physics.Raycast(spawnPoint, Vector3.down, out RaycastHit hit, 200f))
        {
            spawnPoint = hit.point + Vector3.up * 0.1f; // just above ground
        }
        GameObject van = Instantiate(vanPrefab, spawnPoint, Quaternion.identity);
        NetworkObject vanNetObj = van.GetComponent<NetworkObject>();

        if (vanNetObj == null)
        {
            Debug.LogError("Van prefab is missing NetworkObject component!");
            return;
        }

        vanNetObj.Spawn();
        Debug.Log("Van spawned successfully");
    }






    public override void OnNetworkDespawn()
    {
        if (lobbyPanelInstance != null)
        {
            Destroy(lobbyPanelInstance);
        }
    }


    private void OnClientConnected(ulong clientId)
    {

        var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        
        if (playerObject != null)
        {
            playerObjects[clientId] = playerObject;

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                LocalPlayerObject = playerObject;
            }
        }
        Debug.Log($"Client connected: {clientId}");

        if (!IsServer)
        {
            Debug.Log($"Non-server received connection callback for {clientId}");
            return;
        }

        if (NetworkManager.Singleton.ConnectedClients.Count > MaxConnections)
        {
            Debug.Log($"Rejecting client {clientId} - max connections reached");
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
        }

        string playerName;
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            playerName = string.IsNullOrEmpty(nameInputField.text.Trim())
                ? $"Host{clientId}"
                : nameInputField.text.Trim();
        }
        else
        {
            playerName = $"Player{clientId}";
        }

        PlayerDataManager.Instance.RegisterPlayer(clientId, playerName);

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            AddPlayerData(clientId, playerName);
        }
        else
        {
            RequestPlayerNameClientRpc(clientId);
        }

        Debug.Log($"Successfully processed connection for client {clientId}");
    }

    //--- Lobby Data Management ---

    private void CreateLobbyUI()
    {
        if (lobbyPanelInstance != null)
        {
            Destroy(lobbyPanelInstance);
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        lobbyPanelInstance = Instantiate(LobbyPanelPrefab, canvas.transform);
    }

    [ClientRpc]
    private void RequestPlayerNameClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            string name = string.IsNullOrEmpty(nameInputField.text.Trim())
                ? "Player" + clientId
                : nameInputField.text.Trim();
            SubmitPlayerNameServerRpc(name);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPlayerNameServerRpc(string name, ServerRpcParams rpcParams = default)
        => AddPlayerData(rpcParams.Receive.SenderClientId, name);

    private void AddPlayerData(ulong clientId, string name)
        => playerData.Add(new PlayerLobbyData
        {
            clientId = clientId,
            playerName = name,
            isReady = false
        });


    private void OnClientDisconnected(ulong clientId)
    {
        if (playerObjects.ContainsKey(clientId))
        {
            playerObjects.Remove(clientId);
        }

        if (clientId == LocalClientId)
        {
            LocalPlayerObject = null;
        }
        if (!IsServer) return;

        for (int i = 0; i < playerData.Count; i++)
        {
            if (playerData[i].clientId == clientId)
            {
                playerData.RemoveAt(i);
                break;
            }
        }
    }

    private void OnPlayerListChanged(NetworkListEvent<PlayerLobbyData> _) => UpdateLobbyUI();
    private void UpdateLobbyUI()
    {
        Debug.Log("Updating lobby UI with player data...");
        if (lobbyPanelInstance == null) return;

        LobbyManager lobbyManager = lobbyPanelInstance.GetComponentInChildren<LobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.UpdatePlayerList(playerData);
        }
        else
        {
            Debug.LogWarning("LobbyManager component not found on lobbyPanelInstance!");
        }
    }


    public void ToggleReadyStatus() => ToggleReadyServerRpc();

    [ServerRpc(RequireOwnership = false)]
    private void ToggleReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        for (int i = 0; i < playerData.Count; i++)
        {
            if (playerData[i].clientId == rpcParams.Receive.SenderClientId)
            {
                var data = playerData[i];
                data.isReady = !data.isReady;
                playerData[i] = data;
                break;
            }
        }
    }


    // ---- UI Button Handlers -----
    public void OnHostButtonClicked()
    {
        if (!IsPortAvailable())
        {
            UpdateStatus($"Port {port} in use!");
            return;
        }

        if (GetRadminIP() != null)
        {
            transport.SetConnectionData(GetRadminIP(), port);
            UpdateStatus($"Hosting on port {port}\nIP: {GetRadminIP()}");
            Debug.Log($"Hosting on port {port}\nIP: {GetRadminIP()}");
        }
        else
        {
            transport.SetConnectionData(GetLocalIPAddress(), port);
            UpdateStatus($"Hosting on port {port}\nIP: {GetLocalIPAddress()}");
            Debug.Log($"Hosting on port {port}\nIP: {GetLocalIPAddress()}");
        }

        NetworkManager.Singleton.StartHost();
    }

    public List<string> GetAllLocalIPAddresses()
    {
        List<string> ipAddresses = new List<string>();

        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Skip inactive/disconnected interfaces
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            // Skip loopback and non-IPv4 interfaces
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                continue;

            // Get IP properties
            IPInterfaceProperties ipProps = ni.GetIPProperties();
            foreach (UnicastIPAddressInformation ip in ipProps.UnicastAddresses)
            {
                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipAddresses.Add(ip.Address.ToString());
                }
            }
        }

        return ipAddresses;
    }

    public string GetRadminIP()
    {
        foreach (string ip in GetAllLocalIPAddresses())
        {
            if (ip.StartsWith("26.")) // Radmin's typical subnet
                return ip;
        }
        return null;
    }
    public static string GetPublicIPAddress()
    {
        try
        {
            return new System.Net.WebClient().DownloadString("https://api.ipify.org");
        }
        catch { return "Cannot get public IP"; }
    }

    public void OnJoinButtonClicked()
    {
        transport.SetConnectionData(ipInputField.text.Trim(), port);
        NetworkManager.Singleton.StartClient();
    }

    private void UpdateStatus(string message)
        => connectionStatusText.text = message;




    // ---- Utility functions -----
    //This function checks if the specified port is available for use.
    private bool IsPortAvailable()
    {
        try
        {
            using (new UdpClient(port))
                return true;
        }
        catch { return false; }
    }


    //This function gets the local IP address of the host machine.
    public static string GetLocalIPAddress()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "127.0.0.1";
            }
        }
        catch { return "127.0.0.1"; }
    }
}