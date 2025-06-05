using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Utility script for testing disconnection scenarios in Unity networking
/// </summary>
public class DisconnectionTester : MonoBehaviour
{    [Header("Test Settings")]
    [SerializeField] private bool enableDebugUI = true;
    [SerializeField] private KeyCode forceDisconnectKey = KeyCode.J;  // Changed to J for easy debug access
    [SerializeField] private KeyCode simulateHostLossKey = KeyCode.F10;
    [SerializeField] private KeyCode simulateNetworkLossKey = KeyCode.F11;
    
    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Button testDisconnectButton;
    [SerializeField] private TMPro.TMP_Text statusText;
    
    private P2P_Manager p2pManager;
    private bool isTestingDisconnection = false;

    private void Start()
    {
        p2pManager = FindFirstObjectByType<P2P_Manager>();
        
        if (testDisconnectButton != null)
        {
            testDisconnectButton.onClick.AddListener(TestDisconnection);
        }
        
        UpdateStatusText("Disconnection Tester Ready");
    }

    private void Update()
    {
        if (!enableDebugUI) return;

        // Test manual disconnection
        if (Input.GetKeyDown(forceDisconnectKey))
        {
            TestDisconnection();
        }
        
        // Test host loss simulation
        if (Input.GetKeyDown(simulateHostLossKey))
        {
            TestHostLoss();
        }
        
        // Test network loss simulation
        if (Input.GetKeyDown(simulateNetworkLossKey))
        {
            TestNetworkLoss();
        }
    }

    /// <summary>
    /// Test manual disconnection through P2P_Manager
    /// </summary>
    public void TestDisconnection()
    {
        if (isTestingDisconnection) return;
        
        Debug.Log("Testing manual disconnection...");
        UpdateStatusText("Testing Manual Disconnect...");
        
        if (p2pManager != null)
        {
            p2pManager.ManualDisconnect();
        }
        else if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        isTestingDisconnection = true;
        StartCoroutine(ResetTestFlag());
    }

    /// <summary>
    /// Test host loss scenario
    /// </summary>
    public void TestHostLoss()
    {
        if (isTestingDisconnection) return;
        
        Debug.Log("Testing host loss simulation...");
        UpdateStatusText("Testing Host Loss...");
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            // Host shuts down abruptly
            NetworkManager.Singleton.Shutdown();
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            // Client simulates host loss by disconnecting
            NetworkManager.Singleton.Shutdown();
        }
        
        isTestingDisconnection = true;
        StartCoroutine(ResetTestFlag());
    }

    /// <summary>
    /// Test network loss scenario
    /// </summary>
    public void TestNetworkLoss()
    {
        if (isTestingDisconnection) return;
        
        Debug.Log("Testing network loss simulation...");
        UpdateStatusText("Testing Network Loss...");
        
        StartCoroutine(SimulateNetworkLoss());
    }

    /// <summary>
    /// Simulate gradual network loss
    /// </summary>
    private IEnumerator SimulateNetworkLoss()
    {
        isTestingDisconnection = true;
        
        // Simulate network timeout by forcefully shutting down
        if (NetworkManager.Singleton != null)
        {
            yield return new WaitForSeconds(0.5f);
            NetworkManager.Singleton.Shutdown();
        }
        
        yield return ResetTestFlag();
    }

    /// <summary>
    /// Reset the testing flag after a delay
    /// </summary>
    private IEnumerator ResetTestFlag()
    {
        yield return new WaitForSeconds(3f);
        isTestingDisconnection = false;
        UpdateStatusText("Test Complete - Ready for next test");
    }

    /// <summary>
    /// Update status text if available
    /// </summary>
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"DisconnectionTester: {message}");
    }

    /// <summary>
    /// Display debug GUI if enabled
    /// </summary>
    private void OnGUI()
    {
        if (!enableDebugUI) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Disconnection Tester", GUI.skin.box);
        
        if (NetworkManager.Singleton != null)
        {
            GUILayout.Label($"Network State: {GetNetworkState()}");
            GUILayout.Label($"Connected Clients: {NetworkManager.Singleton.ConnectedClients.Count}");
        }
        else
        {
            GUILayout.Label("NetworkManager: Not Available");
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button($"Force Disconnect ({forceDisconnectKey})"))
        {
            TestDisconnection();
        }
        
        if (GUILayout.Button($"Simulate Host Loss ({simulateHostLossKey})"))
        {
            TestHostLoss();
        }
        
        if (GUILayout.Button($"Simulate Network Loss ({simulateNetworkLossKey})"))
        {
            TestNetworkLoss();
        }
        
        GUILayout.Space(10);
        GUILayout.Label($"Status: {(isTestingDisconnection ? "Testing..." : "Ready")}");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    /// <summary>
    /// Get current network state as string
    /// </summary>
    private string GetNetworkState()
    {
        if (NetworkManager.Singleton == null) return "None";
        
        if (NetworkManager.Singleton.IsHost) return "Host";
        if (NetworkManager.Singleton.IsServer) return "Server";
        if (NetworkManager.Singleton.IsClient) return "Client";
        
        return "Disconnected";
    }

    /// <summary>
    /// Validate the disconnection system
    /// </summary>
    public void ValidateDisconnectionSystem()
    {
        Debug.Log("=== Disconnection System Validation ===");
        
        // Check P2P_Manager
        if (p2pManager != null)
        {
            Debug.Log("✓ P2P_Manager found");
        }
        else
        {
            Debug.LogError("✗ P2P_Manager not found");
        }
        
        // Check NetworkManager
        if (NetworkManager.Singleton != null)
        {
            Debug.Log("✓ NetworkManager available");
        }
        else
        {
            Debug.LogError("✗ NetworkManager not available");
        }
        
        // Check DisconnectionSceneManager
        var disconnectionManager = FindFirstObjectByType<DisconnectionSceneManager>();
        if (disconnectionManager != null)
        {
            Debug.Log("✓ DisconnectionSceneManager found");
        }
        else
        {
            Debug.LogWarning("⚠ DisconnectionSceneManager not found (optional)");
        }
        
        // Check LobbyManager
        var lobbyManager = FindFirstObjectByType<LobbyManager>();
        if (lobbyManager != null)
        {
            Debug.Log("✓ LobbyManager found");
        }
        else
        {
            Debug.LogWarning("⚠ LobbyManager not found (scene dependent)");
        }
        
        Debug.Log("=== Validation Complete ===");
    }

    /// <summary>
    /// Test all disconnection scenarios in sequence
    /// </summary>
    public void RunFullDisconnectionTest()
    {
        StartCoroutine(FullTestSequence());
    }
    
    private IEnumerator FullTestSequence()
    {
        Debug.Log("Starting full disconnection test sequence...");
        
        UpdateStatusText("Running Full Test...");
        
        // Test 1: Manual Disconnect
        Debug.Log("Test 1: Manual Disconnect");
        TestDisconnection();
        yield return new WaitForSeconds(5f);
        
        // Test 2: Host Loss (if applicable)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Test 2: Host Loss");
            TestHostLoss();
            yield return new WaitForSeconds(5f);
        }
        
        // Test 3: Network Loss
        Debug.Log("Test 3: Network Loss");
        TestNetworkLoss();
        yield return new WaitForSeconds(5f);
        
        UpdateStatusText("Full Test Complete");
        Debug.Log("Full disconnection test sequence complete!");
    }
}
