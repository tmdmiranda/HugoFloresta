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

    public GameObject Roleta;
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
    [SerializeField] private GameObject spawnPoint;

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
    }    public new bool IsLocalPlayer(NetworkObject obj)
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
            spawnPoint = GameObject.Find("SpawnPos");
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
        SpawnRoleta();
    }

    private Vector3 CalculateSpawnPosition(int index, int totalPlayers)
    {
        float radius = 5f;
        float angle = index * (2f * Mathf.PI / totalPlayers);
        Vector3 center = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;

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

        Vector3 spawnPois = spawnPoint.transform.position;
        if (Physics.Raycast(spawnPois, Vector3.down, out RaycastHit hit, 200f))
        {
            spawnPois = hit.point + Vector3.up * 0.1f; // just above ground
        }
        GameObject van = Instantiate(vanPrefab, spawnPois, Quaternion.identity);
        NetworkObject vanNetObj = van.GetComponent<NetworkObject>();

        if (vanNetObj == null)
        {
            Debug.LogError("Van prefab is missing NetworkObject component!");
            return;
        }

        vanNetObj.Spawn();
        Debug.Log("Van spawned successfully");
    }


    public void SpawnRoleta()
    {
        if (Roleta == null)
        {
            Debug.LogError("tiremistoxdd: ROLETA NULL");
            return;
        }

        Vector3 initialSpawnPosition = new Vector3(900f, 100f, 423f); // atart high
        Vector3 finalSpawnPoint = initialSpawnPosition;

        RaycastHit hit;
        if (Physics.Raycast(initialSpawnPosition, Vector3.down, out hit, 500f)) // adicionei um quito +dist
        {
            finalSpawnPoint = hit.point + (Vector3.up * 0.1f);
            Debug.Log($"hit:{hit.point} final:{finalSpawnPoint}");
        }
        else
        {
            Debug.LogWarning($"raycast fail");
        }

        GameObject roletaInstance = null;
        try
        {
            Debug.Log($"vou instanciar roleta a {finalSpawnPoint}");
            roletaInstance = Instantiate(Roleta, finalSpawnPoint, Quaternion.identity);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ardeu aqui {e.Message}\n{e.StackTrace}");
            if (roletaInstance != null)
                Destroy(roletaInstance);
            return;
        }


        if (roletaInstance == null)
        {
            Debug.LogError("nao consegui spawnar roleta, checkar cenas anteriores.");
            return;
        }

        Debug.Log($"roleta criada: {roletaInstance.name}");

        NetworkObject roletaNetObj = roletaInstance.GetComponent<Unity.Netcode.NetworkObject>();

        if (roletaNetObj == null)
        {
            Debug.LogError("netobj null");
            Destroy(roletaInstance);
            return;
        }

        Debug.Log("correu tdbem?.");
        try
        {
            roletaNetObj.Spawn();
            Debug.Log($"spawnou");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"mega shit: {e.Message}\n{e.StackTrace}");
            Destroy(roletaInstance);
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
        Debug.Log($"Client {clientId} disconnected. IsServer: {IsServer}, LocalClientId: {LocalClientId}");

        // Handle disconnected player cleanup
        if (playerObjects.ContainsKey(clientId))
        {
            var playerObj = playerObjects[clientId];
            if (playerObj != null)
            {
                // Despawn the player object if we're the server
                if (IsServer && playerObj.IsSpawned)
                {
                    playerObj.Despawn();
                }
            }
            playerObjects.Remove(clientId);
        }

        // Handle local player disconnection
        if (clientId == LocalClientId)
        {
            LocalPlayerObject = null;
            Debug.Log("Local player disconnected!");
            
            // If this is a client that got disconnected, return to main menu
            if (!IsServer)
            {
                HandleClientDisconnection();
                return;
            }
        }        // Handle host disconnection for other clients
        if (clientId == NetworkManager.ServerClientId && !IsServer)
        {
            Debug.Log("Host disconnected! Returning to main menu...");
            HandleHostDisconnection();
            return;
        }

        // Server-side cleanup for disconnected clients
        if (IsServer)
        {
            // Remove from player data list
            for (int i = 0; i < playerData.Count; i++)
            {
                if (playerData[i].clientId == clientId)
                {
                    playerData.RemoveAt(i);
                    break;
                }
            }

            // Notify remaining clients about the disconnection
            NotifyPlayerDisconnectedClientRpc(clientId);
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
    }    /// <summary>
    /// Check if the specified port is available for hosting
    /// </summary>
    private bool IsPortAvailable()
    {
        try
        {
            var socket = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
            socket.Start();
            socket.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get the first available local IP address
    /// </summary>
    private string GetLocalIPAddress()
    {
        var addresses = GetAllLocalIPAddresses();
        return addresses.Count > 0 ? addresses[0] : "127.0.0.1";
    }

    /// <summary>
    /// Update the connection status text
    /// </summary>
    private void UpdateStatus(string message)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = message;
        }
        Debug.Log($"Status: {message}");
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

    // ==============================================
    // DISCONNECTION HANDLING METHODS
    // ==============================================

    /// <summary>
    /// Handles client disconnection scenario - returns to lobby/main menu
    /// </summary>
    private void HandleClientDisconnection()
    {
        Debug.Log("Handling client disconnection...");
        StartCoroutine(CleanupAndReturnToMenu("Connection to host lost"));
    }

    /// <summary>
    /// Handles host disconnection scenario - returns all clients to lobby/main menu
    /// </summary>
    private void HandleHostDisconnection()
    {
        Debug.Log("Handling host disconnection...");
        StartCoroutine(CleanupAndReturnToMenu("Host disconnected"));
    }

    /// <summary>
    /// Notifies all clients when a player disconnects
    /// </summary>
    [ClientRpc]
    private void NotifyPlayerDisconnectedClientRpc(ulong disconnectedClientId)
    {
        Debug.Log($"Player {disconnectedClientId} has left the game");
        
        // You can add UI notifications here if needed
        if (connectionStatusText != null)
        {
            connectionStatusText.text = $"Player {disconnectedClientId} disconnected";
        }
    }

    /// <summary>
    /// Comprehensive cleanup and return to main menu
    /// </summary>
    private IEnumerator CleanupAndReturnToMenu(string reason = "Disconnected")
    {
        Debug.Log($"Starting cleanup and return to menu. Reason: {reason}");

        // Update status text
        if (connectionStatusText != null)
        {
            connectionStatusText.text = reason;
        }

        // Show cursor for menu interaction
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        // Notify lobby manager about disconnection
        NotifyLobbyManagerDisconnection(reason);

        // Cleanup networking
        CleanupNetworking();

        // Wait a frame to ensure cleanup is complete
        yield return null;        // Handle disconnection directly
        HandleSceneTransition();
    }

    /// <summary>
    /// Notify lobby manager about disconnection
    /// </summary>
    private void NotifyLobbyManagerDisconnection(string reason)
    {
        if (lobbyPanelInstance != null)
        {
            LobbyManager lobbyManager = lobbyPanelInstance.GetComponentInChildren<LobbyManager>();
            if (lobbyManager != null)
            {
                lobbyManager.OnDisconnected(reason);
            }
        }
    }

    /// <summary>
    /// Handle scene transition when DisconnectionSceneManager is not available
    /// </summary>
    private void HandleSceneTransition()
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        if (currentSceneName == "MainScene")
        {
            // Try to find and load the first scene (usually main menu)
            if (UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings > 0)
            {
                string firstScenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(0);
                if (!string.IsNullOrEmpty(firstScenePath))
                {
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(firstScenePath);
                    Debug.Log($"Loading scene: {sceneName}");
                    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                    return;
                }
            }
            
            // Fallback: recreate lobby UI in current scene
            RecreateMainMenuUI();
        }
        else
        {
            // We're already in the lobby scene, just reset the UI
            ResetLobbyUI();
        }
    }

    /// <summary>
    /// Cleanup all networking components and references
    /// </summary>
    private void CleanupNetworking()
    {
        Debug.Log("Cleaning up networking...");

        try
        {
            // Clear player data
            if (playerData != null && IsServer)
            {
                playerData.Clear();
            }

            // Clear player objects
            foreach (var kvp in playerObjects)
            {
                if (kvp.Value != null && kvp.Value.IsSpawned)
                {
                    try
                    {
                        if (IsServer)
                        {
                            kvp.Value.Despawn();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Error despawning player object: {ex.Message}");
                    }
                }
            }
            playerObjects.Clear();

            // Reset local references
            LocalPlayerObject = null;
            LocalClientId = 0;

            // Shutdown networking
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsHost)
                {
                    NetworkManager.Singleton.Shutdown();
                }
                else if (NetworkManager.Singleton.IsClient)
                {
                    NetworkManager.Singleton.Shutdown();
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during networking cleanup: {ex.Message}");
        }
    }

    /// <summary>
    /// Recreate the main menu UI when returning from game scene
    /// </summary>
    private void RecreateMainMenuUI()
    {
        Debug.Log("Recreating main menu UI...");

        // Destroy any existing lobby panel
        if (lobbyPanelInstance != null)
        {
            Destroy(lobbyPanelInstance);
            lobbyPanelInstance = null;
        }

        // Find or create canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Recreate lobby UI
        CreateLobbyUI();

        // Reset connection status
        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Disconnected - Ready to reconnect";
        }

        // Make sure UI elements are reset
        ResetUIElements();
    }

    /// <summary>
    /// Reset lobby UI to initial state
    /// </summary>
    private void ResetLobbyUI()
    {
        Debug.Log("Resetting lobby UI...");

        // Clear player data display if in lobby
        if (lobbyPanelInstance != null)
        {
            LobbyManager lobbyManager = lobbyPanelInstance.GetComponentInChildren<LobbyManager>();
            if (lobbyManager != null)
            {
                // Create empty player list to clear the UI
                var emptyList = new NetworkList<PlayerLobbyData>();
                lobbyManager.UpdatePlayerList(emptyList);
            }
        }

        // Reset connection status
        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Disconnected - Ready to reconnect";
        }

        ResetUIElements();
    }

    /// <summary>
    /// Reset UI elements to their default state
    /// </summary>
    private void ResetUIElements()
    {
        // Enable input fields
        if (nameInputField != null)
        {
            nameInputField.interactable = true;
        }
        if (ipInputField != null)
        {
            ipInputField.interactable = true;
        }

        // Reset any other UI elements as needed
    }

    /// <summary>
    /// Override OnDestroy to ensure proper cleanup
    /// </summary>
    public override void OnDestroy()
    {
        base.OnDestroy();
        
        // Unsubscribe from events to prevent memory leaks
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (playerData != null)
        {
            playerData.OnListChanged -= OnPlayerListChanged;
        }
    }

    /// <summary>
    /// Override OnNetworkDespawn for additional cleanup
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Debug.Log("P2P_Manager despawning...");
        
        // Additional cleanup if needed
        CleanupNetworking();
    }

    /// <summary>
    /// Manually trigger disconnection cleanup (can be called from UI)
    /// </summary>
    public void ManualDisconnect()
    {
        Debug.Log("Manual disconnect requested");
        
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
        
        StartCoroutine(CleanupAndReturnToMenu("Manual disconnect"));
    }

    /// <summary>
    /// Add a disconnect button to the UI (can be called from external UI managers)
    /// </summary>
    public void AddDisconnectButton(UnityEngine.UI.Button disconnectButton)
    {
        if (disconnectButton != null)
        {
            disconnectButton.onClick.AddListener(ManualDisconnect);
        }
    }

}