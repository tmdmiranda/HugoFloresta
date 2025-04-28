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



public class P2P_Manager : NetworkBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField nameInputField;
    public TMP_InputField ipInputField;
    public TMP_Text hostIp;
    public ushort port = 25000;
    public TMP_Text connectionStatusText;
    public int MaxConnections = 8;
    public GameObject LobbyPanelPrefab;
    public GameObject PlayerPrefab;

    private UnityTransport transport;
    private NetworkList<PlayerLobbyData> playerData;
    private GameObject lobbyPanelInstance;
    private NetworkVariable<bool> gameInProgress = new NetworkVariable<bool>(false);

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

    private void Awake() => playerData = new NetworkList<PlayerLobbyData>();

    private bool isNetworkInitialized = false;

    private void Start()
    {
        StartCoroutine(InitializeNetwork());
        // Remove the direct RegisterSceneCallbacks() call from here
    }



    private IEnumerator InitializeNetwork()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);

        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
            transport = NetworkManager.Singleton.gameObject.AddComponent<UnityTransport>();

        transport.SetConnectionData("0.0.0.0", port);
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }


    public void StartGame()
    {
        Debug.Log("Check 1 Done!");
        if (!IsOwner) return; // Only host starts the game
        NetworkManager.Singleton.SceneManager.LoadScene("MartinP2P", LoadSceneMode.Single);
        StartCoroutine(SpawnPlayersOneByOne());
    }


    private IEnumerator SpawnPlayersOneByOne()
    {
        Debug.Log("Check 2 Done!");
        if (!IsOwner) yield break;

        var clients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        clients.Sort();
        Debug.Log("Check 3 Done!");
        for (int i = 0; i < clients.Count; i++)
        {
            Debug.Log($"Check 4 Done! {i}");
            ulong clientId = clients[i];

            // Get spawn position from SpawnManager
            Vector3 spawnPos = new Vector3(915f, 50f, 418f);
            Debug.Log($"Spawning at: {spawnPos}");

            GameObject player = Instantiate(
                PlayerPrefab,
                spawnPos,
                Quaternion.identity
            );

            NetworkObject netObj = player.GetComponent<NetworkObject>();
            netObj.SpawnWithOwnership(clientId);

            Debug.Log($"Spawned player for client {clientId} at {spawnPos}");

            if (i < clients.Count - 1)
                yield return new WaitForSeconds(1f);
        }
    }

    private void SpawnMyPlayer()
    {
        // 🔥 Find ALL spawn points in the scene dynamically
        GameObject[] spawnObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnObjects.Length == 0)
        {
            Debug.LogError("NO SPAWN POINTS FOUND! Did you tag them?");
            return;
        }

        // Pick a spawn based on your ID (or random)
        int mySpawnIndex = (int)NetworkManager.Singleton.LocalClientId % spawnObjects.Length;
        Vector3 spawnPos = spawnObjects[mySpawnIndex].transform.position;

        // Spawn player (Netcode syncs it)
        GameObject player = Instantiate(PlayerPrefab, spawnPos, Quaternion.identity);
        player.GetComponent<NetworkObject>().Spawn();
    }

    private bool IsSceneLoaded(string sceneName)
    {
        return SceneManager.GetSceneByName(sceneName).isLoaded;
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


    private void CreateLobbyUI()
    {
        if (lobbyPanelInstance == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            lobbyPanelInstance = Instantiate(LobbyPanelPrefab, canvas.transform);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
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
        if (lobbyPanelInstance == null) return;
        if (lobbyPanelInstance.TryGetComponent<LobbyManager>(out var lobbyManager))
        {
            lobbyManager.UpdatePlayerList(playerData);
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



    public void OnHostButtonClicked()
    {
        if (!IsPortAvailable())
        {
            UpdateStatus($"Port {port} in use!");
            return;
        }

        transport.SetConnectionData("0.0.0.0", port);
        NetworkManager.Singleton.StartHost();
        UpdateStatus($"Hosting on port {port}\nIP: {GetLocalIPAddress()}");
        hostIp.text = $"Host IP: {GetLocalIPAddress()}";
    }

    public void OnJoinButtonClicked()
    {
        transport.SetConnectionData(ipInputField.text.Trim(), port);
        NetworkManager.Singleton.StartClient();
    }

    private void UpdateStatus(string message)
        => connectionStatusText.text = message;

    private bool IsPortAvailable()
    {
        try
        {
            using (new UdpClient(port))
                return true;
        }
        catch { return false; }
    }

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