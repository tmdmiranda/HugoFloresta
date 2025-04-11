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
    public TMP_InputField nameInputField; // Input field for player name
    public TMP_InputField ipInputField;

    public TMP_Text hostIp;

    public ushort port = 25000;
    public TMP_Text connectionStatusText;

    private UnityTransport transport;
    private UdpClient testUdpListener;
    private bool isUdpPortAvailable = true;
    private LobbyManager lobbyManager;
    public int MaxConnections = 8; // Maximum number of connections allowed
    public TopDownViewInteract topDownViewInteract; // Reference to the Roleta script

    public GameObject LobbyPanelprefab; // Reference to the lobby panel prefab

    private NetworkList<FixedString32Bytes> playerNames;

    private void Awake()
    {
        playerNames = new NetworkList<FixedString32Bytes>();
    }
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected!");
        if (!NetworkManager.Singleton.IsServer) return;

        // Check max connections
        if (NetworkManager.Singleton.ConnectedClients.Count > MaxConnections)
        {
            Debug.Log($"Max connections reached. Rejecting client {clientId}");
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
        }

        if (IsOwner == true)
        {
            playerNames.Add("Owner");
            UpdateLobbyUI();
            return;
        }
        else
        {
            // Request player name from the client
            Debug.Log($"Requesting player name from client {clientId}");
            RequestPlayerNameClientRpc(clientId);
        }

    }

    [ClientRpc]
    private void RequestPlayerNameClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            string name = nameInputField.text.Trim();
            Debug.Log($"Requesting player name from client {clientId}");
            if (string.IsNullOrEmpty(name)) name = "Player" + clientId;
            SubmitPlayerNameServerRpc(name);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPlayerNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {


        ulong clientId = rpcParams.Receive.SenderClientId;
        playerNames.Add(name);

        Debug.Log($"Client {clientId} connected with name: {name}");
        UpdateLobbyUI();
    }

    private void UpdateLobbyUI()
    {
        if (lobbyManager == null)
        {
            var lobbyObj = Instantiate(LobbyPanelprefab);
            lobbyManager = lobbyObj.GetComponent<LobbyManager>();
        }

        lobbyManager.UpdatePlayerList(playerNames);
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

    public void StartGame()
    {
        {
            topDownViewInteract.inGame = true; // Set inGame to true in Roleta script
        }
    }



    public void OnHostButtonClicked()
    {
        Instantiate(LobbyPanelprefab);
        if (!IsPortAvailable())
        {
            UpdateStatus($"Port {port} is in use! Try another port.");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            UpdateStatus("Already hosting!");
            return;
        }
        // Configure transport
        transport.SetConnectionData(
            "0.0.0.0",  // Listen on all interfaces
            port,
            "0.0.0.0"   // Explicit listen address
        );

        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        if (NetworkManager.Singleton.StartHost())
        {
            UpdateStatus($"Hosting on UDP port {port}\nLocal IP: {GetLocalIPAddress()}");
            topDownViewInteract.inGame = true; // Set inGame to true in Roleta script
            hostIp.text = $"Host IP: {GetLocalIPAddress()}";

        }
        else
        {
            UpdateStatus("Host failed to start!");
        }

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

    // Optional: Continuously check if port is receiving data

    private void UpdateStatus(string message)
    {
        Debug.Log(message);
        if (connectionStatusText != null)
            connectionStatusText.text = message;
    }

    public void OnJoinButtonClicked()
    {
        Instantiate(LobbyPanelprefab);
        string targetIP = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(targetIP))
        {
            Debug.LogWarning("Please enter a valid IP address");
            return;
        }

        transport.SetConnectionData(targetIP, port);

        if (NetworkManager.Singleton.StartClient())
        {
            topDownViewInteract.inGame = true;
            Debug.Log($"UDP Client started to connect to {targetIP}:{port}");

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