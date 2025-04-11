using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;

public class P2P_Manager : NetworkBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField nameInputField;
    public TMP_InputField ipInputField;
    public TMP_Text hostIp;
    public ushort port = 25000;
    public TMP_Text connectionStatusText;
    public int MaxConnections = 8;
    public TopDownViewInteract topDownViewInteract;
    public GameObject LobbyPanelPrefab;

    private UnityTransport transport;
    private NetworkList<FixedString32Bytes> playerNames;
    private GameObject lobbyPanelInstance;

    private void Awake()
    {
        playerNames = new NetworkList<FixedString32Bytes>();
    }

    private void Start()
    {
        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("UnityTransport component missing!");
            return;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        playerNames.OnListChanged += OnPlayerListChanged;
        
        // Create lobby UI for all players
        CreateLobbyUI();
        UpdateLobbyUI();
    }

    private void CreateLobbyUI()
    {
        if (lobbyPanelInstance != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        lobbyPanelInstance = Instantiate(LobbyPanelPrefab, canvas.transform);
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected!");
        if (!IsServer) return;

        // Check max connections
        if (NetworkManager.Singleton.ConnectedClients.Count > MaxConnections)
        {
            Debug.Log($"Max connections reached. Rejecting client {clientId}");
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
        }

        if (IsOwner)
        {
            playerNames.Add(nameInputField.text.Trim());
        }
        else
        {
            RequestPlayerNameClientRpc(clientId);
        }
    }

    [ClientRpc]
    private void RequestPlayerNameClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            string name = nameInputField.text.Trim();
            if (string.IsNullOrEmpty(name)) name = "Player" + clientId;
            SubmitPlayerNameServerRpc(name);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPlayerNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {
        playerNames.Add(name);
        Debug.Log($"Client {rpcParams.Receive.SenderClientId} connected with name: {name}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < playerNames.Count; i++)
        {
            if (playerNames[i].ToString().Contains(clientId.ToString()))
            {
                playerNames.RemoveAt(i);
                break;
            }
        }
    }

    private void OnPlayerListChanged(NetworkListEvent<FixedString32Bytes> changeEvent)
    {
        UpdateLobbyUI();
    }

    private void UpdateLobbyUI()
    {
        if (lobbyPanelInstance == null) return;

        LobbyManager lobbyManager = lobbyPanelInstance.GetComponentInChildren<LobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.UpdatePlayerList(playerNames);
        }
    }

    public void StartGame()
    {
        if (IsServer)
        {
            topDownViewInteract.inGame = true;
            // Add any additional game start logic here
        }
    }

    public void OnHostButtonClicked()
    {
        if (!IsPortAvailable())
        {
            UpdateStatus($"Port {port} is in use! Try another port.");
            return;
        }

        transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");

        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        if (NetworkManager.Singleton.StartHost())
        {
            UpdateStatus($"Hosting on UDP port {port}\nLocal IP: {GetLocalIPAddress()}");
            hostIp.text = $"Host IP: {GetLocalIPAddress()}";
            playerNames.Add(nameInputField.text.Trim());
        }
        else
        {
            UpdateStatus("Host failed to start!");
        }
    }

    public void OnJoinButtonClicked()
    {
        string targetIP = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(targetIP))
        {
            Debug.LogWarning("Please enter a valid IP address");
            return;
        }

        transport.SetConnectionData(targetIP, port);

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log($"Connecting to {targetIP}:{port}");
        }
        else
        {
            Debug.LogError("Failed to start client!");
        }
    }

    private void OnServerStarted()
    {
        Debug.Log("UDP Server started successfully!");
    }

        private void UpdateStatus(string message)
    {
        Debug.Log(message);
        if (connectionStatusText != null)
            connectionStatusText.text = message;
    }

       public bool IsPortAvailable()
    {
        try
        {
            // Test if port is available (but don't keep it open)
            using (var testSocket = new UdpClient(port))
            {
                return true;
            }
        }
        catch (SocketException)
        {
            return false;
        }
    }
    // Helper method to get local IP address
    public static string GetLocalIPAddress()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530); // Google DNS
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }
        catch
        {
            // Fallback to previous method
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
    }

    // Debug method to test UDP port availability
}