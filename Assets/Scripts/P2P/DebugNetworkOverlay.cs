using UnityEngine;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Simple debug overlay for testing disconnection in your scenes
/// Shows network status and provides visual feedback for the J key disconnect
/// </summary>
public class DebugNetworkOverlay : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugOverlay = true;
    [SerializeField] private KeyCode disconnectKey = KeyCode.J;
    
    [Header("UI References (Optional)")]
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private Canvas debugCanvas;
    
    private P2P_Manager p2pManager;
    private bool isDisconnecting = false;
    private float disconnectCooldown = 0f;

    private void Start()
    {
        p2pManager = FindFirstObjectByType<P2P_Manager>();
        
        // Create debug UI if not provided
        if (showDebugOverlay && debugText == null)
        {
            CreateDebugUI();
        }
    }

    private void Update()
    {
        if (disconnectCooldown > 0f)
        {
            disconnectCooldown -= Time.deltaTime;
        }

        // Handle J key disconnect
        if (Input.GetKeyDown(disconnectKey) && !isDisconnecting && disconnectCooldown <= 0f)
        {
            StartDisconnect();
        }

        // Update debug display
        if (showDebugOverlay && debugText != null)
        {
            UpdateDebugDisplay();
        }
    }

    private void StartDisconnect()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("No active network connection to disconnect from");
            ShowTemporaryMessage("No network connection");
            return;
        }

        isDisconnecting = true;
        disconnectCooldown = 3f; // Prevent spam clicking
        
        string networkState = GetNetworkState();
        Debug.Log($"🔌 DISCONNECT TRIGGERED! Current state: {networkState}");
        
        ShowTemporaryMessage($"Disconnecting... ({networkState})");

        // Try different disconnect methods
        if (p2pManager != null)
        {
            p2pManager.ManualDisconnect();
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Reset flag after delay
        Invoke(nameof(ResetDisconnectFlag), 2f);
    }

    private void ResetDisconnectFlag()
    {
        isDisconnecting = false;
    }

    private void ShowTemporaryMessage(string message)
    {
        if (debugText != null)
        {
            StartCoroutine(ShowMessageCoroutine(message, 2f));
        }
    }

    private System.Collections.IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        string originalText = debugText.text;
        debugText.text = message;
        debugText.color = Color.yellow;
        
        yield return new WaitForSeconds(duration);
        
        debugText.color = Color.white;
        // Don't restore original text as it will be updated by UpdateDebugDisplay
    }

    private void UpdateDebugDisplay()
    {
        if (debugText == null) return;

        string status = "";
        
        // Network status
        if (NetworkManager.Singleton != null)
        {
            status += $"Network: {GetNetworkState()}\n";
            status += $"Clients: {NetworkManager.Singleton.ConnectedClients.Count}\n";
            
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
            {
                status += $"ID: {NetworkManager.Singleton.LocalClientId}\n";
            }
        }
        else
        {
            status += "Network: Not Available\n";
        }

        // Scene info
        status += $"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}\n";

        // Disconnect info
        if (isDisconnecting)
        {
            status += $"<color=yellow>DISCONNECTING...</color>\n";
        }
        else if (disconnectCooldown > 0f)
        {
            status += $"<color=orange>Cooldown: {disconnectCooldown:F1}s</color>\n";
        }
        else
        {
            status += $"Press <color=lime>{disconnectKey}</color> to disconnect\n";
        }

        debugText.text = status;
    }

    private string GetNetworkState()
    {
        if (NetworkManager.Singleton == null) return "None";
        
        if (NetworkManager.Singleton.IsHost) return "Host";
        if (NetworkManager.Singleton.IsServer) return "Server";  
        if (NetworkManager.Singleton.IsClient) return "Client";
        
        return "Disconnected";
    }

    private void CreateDebugUI()
    {
        // Find or create canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DebugCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Make sure it's on top
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Create debug text
        GameObject textObj = new GameObject("DebugNetworkText");
        textObj.transform.SetParent(canvas.transform, false);
        
        debugText = textObj.AddComponent<TMP_Text>();
        debugText.text = "Debug Network Overlay";
        debugText.fontSize = 14;
        debugText.color = Color.white;
        debugText.fontStyle = FontStyles.Bold;
        
        // Position in top-left corner
        RectTransform rectTransform = debugText.rectTransform;
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(10, -10);
        rectTransform.sizeDelta = new Vector2(300, 200);

        // Add background for readability
        GameObject bgObj = new GameObject("DebugBackground");
        bgObj.transform.SetParent(textObj.transform, false);
        bgObj.transform.SetAsFirstSibling();
        
        var bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);
        
        var bgRect = bgImage.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        Debug.Log("✓ Debug Network Overlay created");
    }

    /// <summary>
    /// Toggle the debug overlay on/off
    /// </summary>
    public void ToggleDebugOverlay()
    {
        showDebugOverlay = !showDebugOverlay;
        
        if (debugText != null)
        {
            debugText.gameObject.SetActive(showDebugOverlay);
        }
    }

    /// <summary>
    /// Manually trigger disconnect (can be called from UI buttons)
    /// </summary>
    public void ManualDisconnect()
    {
        StartDisconnect();
    }

    private void OnGUI()
    {
        // Fallback GUI if no TMP_Text is available
        if (showDebugOverlay && debugText == null)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Network Debug", GUI.skin.box);
            
            if (NetworkManager.Singleton != null)
            {
                GUILayout.Label($"State: {GetNetworkState()}");
                GUILayout.Label($"Clients: {NetworkManager.Singleton.ConnectedClients.Count}");
            }
            else
            {
                GUILayout.Label("Network: Not Available");
            }
            
            GUILayout.Label($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            
            if (isDisconnecting)
            {
                GUILayout.Label("DISCONNECTING...");
            }
            else
            {
                GUILayout.Label($"Press {disconnectKey} to disconnect");
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
