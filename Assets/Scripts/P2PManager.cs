using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;

public class P2P_Manager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField ipInputField;
    public ushort port = 25000;
    public TextMeshProUGUI connectionStatusText;

    private UnityTransport transport;
    private UdpClient testUdpListener;
    private bool isUdpPortAvailable = true;



    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"Client connected: {clientId}");
        }
    
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

    public void OnHostButtonClicked()
    {
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
        string targetIP = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(targetIP))
        {
            Debug.LogWarning("Please enter a valid IP address");
            return;
        }

        transport.SetConnectionData(targetIP, port);

        if (NetworkManager.Singleton.StartClient())
        {
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

    // Debug method to test UDP port availability
}