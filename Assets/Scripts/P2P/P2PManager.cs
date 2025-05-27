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


    private IEnumerator SpawnPlayersOneByOne()
    {
        Debug.Log("Starting player spawn sequence...");
        if (!IsServer) yield break;

        // Ensure prefab is registered
        RegisterPlayerPrefab();
        if (!isPlayerPrefabRegistered)
        {
            Debug.LogError("Cannot spawn players - prefab not registered!");
            yield break;
        }

        // Wait an additional frame to ensure everything is ready
        yield return null;

        var clients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        clients.Sort();

        Debug.Log($"Will spawn {clients.Count} players");

        for (int i = 0; i < clients.Count; i++)
        {
            ulong clientId = clients[i];

            // Skip if player already exists
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
                client.PlayerObject != null)
            {
                Debug.Log($"Player already exists for client {clientId}, skipping spawn.");
                continue;
            }

            Debug.Log($"Spawning player for client {clientId} ({i + 1}/{clients.Count})");

            Vector3 spawnPos = new Vector3(915f, 50f, 418f);

            // In SpawnPlayersOneByOne(), change the instantiation:
            GameObject player = Instantiate(PlayerPrefab.Prefab, spawnPos, Quaternion.identity);
            NetworkObject netObj = player.GetComponent<NetworkObject>();

            if (netObj == null)
            {
                Debug.LogError("PlayerPrefab is missing NetworkObject component!");
                continue;
            }

            netObj.SpawnWithOwnership(clientId, true); // true = destroy with owner
            Debug.Log($"Successfully spawned player for client {clientId}");

            yield return new WaitForSeconds(0.5f); // Reduced delay between spawns
        }

        Debug.Log("Finished spawning all players");
    }




    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        CreateLobbyUI();
        playerData.OnListChanged += OnPlayerListChanged;
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
        Debug.Log($"Client connected: {clientId}");
        if (!IsServer) return;

        if (NetworkManager.Singleton.ConnectedClients.Count > MaxConnections)
        {
            Debug.Log($"Rejecting client {clientId} - max connections reached");
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
        }

        Debug.Log($"Processing connection for client {clientId}");
        if (!IsServer) return;

        if (NetworkManager.Singleton.ConnectedClients.Count > MaxConnections)
        {
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
        }

        if (clientId == NetworkManager.Singleton.LocalClientId)
            AddPlayerData(clientId, nameInputField.text.Trim());
        else
            RequestPlayerNameClientRpc(clientId);
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