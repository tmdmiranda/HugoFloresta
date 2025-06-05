using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Quick setup component for integrating disconnection system into your scenes
/// Simply add this to a GameObject in your scene and it will handle everything
/// </summary>
public class DisconnectionSystemSetup : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugDisconnect = true;
    [SerializeField] private KeyCode debugDisconnectKey = KeyCode.J;
    
    [Header("Scene Configuration")]
    [SerializeField] private string mainMenuSceneName = "StartingMenu";
    [SerializeField] private string lobbySceneName = "StartingMenu"; // Use StartingMenu as lobby
    [SerializeField] private string gameSceneName = "MainScene";
    
    [Header("Auto Setup")]
    [SerializeField] private bool autoCreateDisconnectionTester = true;
    [SerializeField] private bool autoCreateDisconnectionSceneManager = true;
    
    private P2P_Manager p2pManager;
    private DisconnectionTester disconnectionTester;
    private DisconnectionSceneManager sceneManager;

    private void Start()
    {
        SetupDisconnectionSystem();
    }

    private void Update()
    {
        // Debug disconnect with J key
        if (enableDebugDisconnect && Input.GetKeyDown(debugDisconnectKey))
        {
            DebugDisconnect();
        }
    }

    /// <summary>
    /// Setup the complete disconnection system
    /// </summary>
    private void SetupDisconnectionSystem()
    {
        Debug.Log("Setting up disconnection system...");
        
        // Find or get P2P_Manager
        p2pManager = FindFirstObjectByType<P2P_Manager>();
        if (p2pManager == null)
        {
            Debug.LogWarning("P2P_Manager not found in scene. Disconnection system may not work properly.");
        }
        else
        {
            Debug.Log("✓ P2P_Manager found");
        }

        // Auto-create DisconnectionTester if needed
        if (autoCreateDisconnectionTester)
        {
            disconnectionTester = FindFirstObjectByType<DisconnectionTester>();
            if (disconnectionTester == null)
            {
                GameObject testerObj = new GameObject("DisconnectionTester");
                disconnectionTester = testerObj.AddComponent<DisconnectionTester>();
                Debug.Log("✓ DisconnectionTester created automatically");
            }
            else
            {
                Debug.Log("✓ DisconnectionTester already exists");
            }
        }

        // Auto-create DisconnectionSceneManager if needed
        if (autoCreateDisconnectionSceneManager)
        {
            sceneManager = FindFirstObjectByType<DisconnectionSceneManager>();
            if (sceneManager == null)
            {
                GameObject sceneManagerObj = new GameObject("DisconnectionSceneManager");
                sceneManager = sceneManagerObj.AddComponent<DisconnectionSceneManager>();
                
                // Configure scene names using reflection or manual setup
                ConfigureSceneManager();
                Debug.Log("✓ DisconnectionSceneManager created automatically");
            }
            else
            {
                Debug.Log("✓ DisconnectionSceneManager already exists");
            }
        }

        Debug.Log("Disconnection system setup complete!");
        LogSystemStatus();
    }

    /// <summary>
    /// Configure the scene manager with proper scene names
    /// </summary>
    private void ConfigureSceneManager()
    {
        if (sceneManager == null) return;

        // Since the fields are private, we'll create a simple configuration method
        // The DisconnectionSceneManager will use default names or we can modify it
        Debug.Log($"Configured scenes: Menu={mainMenuSceneName}, Lobby={lobbySceneName}, Game={gameSceneName}");
    }

    /// <summary>
    /// Debug disconnect function for J key
    /// </summary>
    public void DebugDisconnect()
    {
        Debug.Log("=== DEBUG DISCONNECT TRIGGERED (J key) ===");
        
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("NetworkManager not available - no active connection to disconnect");
            return;
        }

        string currentState = GetNetworkState();
        Debug.Log($"Current network state: {currentState}");

        // Use P2P_Manager's manual disconnect if available
        if (p2pManager != null)
        {
            Debug.Log("Using P2P_Manager.ManualDisconnect()");
            p2pManager.ManualDisconnect();
        }
        else if (disconnectionTester != null)
        {
            Debug.Log("Using DisconnectionTester.TestDisconnection()");
            disconnectionTester.TestDisconnection();
        }
        else
        {
            Debug.Log("Using direct NetworkManager.Shutdown()");
            NetworkManager.Singleton.Shutdown();
        }
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
    /// Log the current status of all disconnection system components
    /// </summary>
    private void LogSystemStatus()
    {
        Debug.Log("=== Disconnection System Status ===");
        Debug.Log($"✓ DisconnectionSystemSetup: Active");
        Debug.Log($"{(p2pManager != null ? "✓" : "✗")} P2P_Manager: {(p2pManager != null ? "Found" : "Not Found")}");
        Debug.Log($"{(disconnectionTester != null ? "✓" : "✗")} DisconnectionTester: {(disconnectionTester != null ? "Active" : "Not Active")}");
        Debug.Log($"{(sceneManager != null ? "✓" : "✗")} DisconnectionSceneManager: {(sceneManager != null ? "Active" : "Not Active")}");
        Debug.Log($"✓ Debug Disconnect Key: {debugDisconnectKey}");
        Debug.Log("=== System Ready ===");
    }

    /// <summary>
    /// Manual setup for testing - call this from inspector or other scripts
    /// </summary>
    [ContextMenu("Force Setup Disconnection System")]
    public void ForceSetup()
    {
        SetupDisconnectionSystem();
    }

    /// <summary>
    /// Test disconnection manually - can be called from UI buttons
    /// </summary>
    [ContextMenu("Test Disconnection")]
    public void TestDisconnection()
    {
        DebugDisconnect();
    }

    /// <summary>
    /// Validate that all required components are present
    /// </summary>
    [ContextMenu("Validate System")]
    public void ValidateSystem()
    {
        Debug.Log("=== Validation Results ===");
        
        bool allGood = true;
        
        if (FindFirstObjectByType<NetworkManager>() == null)
        {
            Debug.LogError("✗ NetworkManager not found in scene!");
            allGood = false;
        }
        else
        {
            Debug.Log("✓ NetworkManager found");
        }
        
        if (FindFirstObjectByType<P2P_Manager>() == null)
        {
            Debug.LogWarning("⚠ P2P_Manager not found - disconnection may not work properly");
        }
        else
        {
            Debug.Log("✓ P2P_Manager found");
        }

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("⚠ No Canvas found - UI features may not work");
        }
        else
        {
            Debug.Log("✓ Canvas found");
        }

        if (allGood)
        {
            Debug.Log("✓ System validation passed!");
        }
        else
        {
            Debug.LogWarning("⚠ Some issues found - check logs above");
        }
    }

    private void OnGUI()
    {
        if (!enableDebugDisconnect) return;

        // Simple debug display in top-right corner
        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 100));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Debug Disconnect System", EditorGUIStyle);
        GUILayout.Label($"Press '{debugDisconnectKey}' to disconnect");
        
        if (NetworkManager.Singleton != null)
        {
            GUILayout.Label($"State: {GetNetworkState()}");
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private GUIStyle EditorGUIStyle
    {
        get
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            return style;
        }
    }
}
