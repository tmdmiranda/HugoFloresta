using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Handles smooth scene transitions and disconnection scenarios
/// </summary>
public class DisconnectionSceneManager : MonoBehaviour
{    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "StartingMenu";  // Updated to match your scene
    [SerializeField] private string lobbySceneName = "StartingMenu";     // Using StartingMenu as lobby
    [SerializeField] private float transitionDelay = 1f;
    
    [Header("UI")]
    [SerializeField] private GameObject disconnectionOverlay;
    [SerializeField] private TMPro.TMP_Text disconnectionText;
    
    public static DisconnectionSceneManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Subscribe to network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnNetworkDisconnected;
        }
    }
    
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnNetworkDisconnected;
        }
    }
    
    /// <summary>
    /// Handle network disconnection events
    /// </summary>
    private void OnNetworkDisconnected(ulong clientId)
    {
        // Only handle local player disconnection
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            
            // If we're in the game scene, return to lobby/main menu
            if (currentScene == "MainScene")
            {
                StartCoroutine(HandleDisconnectionTransition("Connection lost"));
            }
        }
    }
    
    /// <summary>
    /// Handle disconnection and scene transition
    /// </summary>
    public void HandleDisconnection(string reason = "Disconnected")
    {
        StartCoroutine(HandleDisconnectionTransition(reason));
    }
    
    /// <summary>
    /// Smoothly transition back to main menu on disconnection
    /// </summary>
    private IEnumerator HandleDisconnectionTransition(string reason)
    {
        Debug.Log($"Handling disconnection transition: {reason}");
        
        // Show disconnection overlay
        ShowDisconnectionOverlay(reason);
        
        // Wait for transition delay
        yield return new WaitForSeconds(transitionDelay);
        
        // Clean up networking
        CleanupNetworking();
        
        // Determine target scene
        string targetScene = DetermineTargetScene();
        
        // Load target scene
        if (!string.IsNullOrEmpty(targetScene) && Application.CanStreamedLevelBeLoaded(targetScene))
        {
            Debug.Log($"Loading scene: {targetScene}");
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogWarning($"Target scene '{targetScene}' not found, staying in current scene");
            HideDisconnectionOverlay();
        }
    }
    
    /// <summary>
    /// Show disconnection overlay with message
    /// </summary>
    private void ShowDisconnectionOverlay(string message)
    {
        if (disconnectionOverlay != null)
        {
            disconnectionOverlay.SetActive(true);
        }
        
        if (disconnectionText != null)
        {
            disconnectionText.text = message;
        }
        
        // Show cursor for user interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    /// <summary>
    /// Hide disconnection overlay
    /// </summary>
    private void HideDisconnectionOverlay()
    {
        if (disconnectionOverlay != null)
        {
            disconnectionOverlay.SetActive(false);
        }
    }
    
    /// <summary>
    /// Clean up networking components
    /// </summary>
    private void CleanupNetworking()
    {
        try
        {
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
                {
                    NetworkManager.Singleton.Shutdown();
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error cleaning up networking: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Determine which scene to load based on current context
    /// </summary>
    private string DetermineTargetScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        
        // If we have a specific main menu scene, use it
        if (!string.IsNullOrEmpty(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            return mainMenuSceneName;
        }
        
        // If we have a lobby scene, use it
        if (!string.IsNullOrEmpty(lobbySceneName) && Application.CanStreamedLevelBeLoaded(lobbySceneName))
        {
            return lobbySceneName;
        }
        
        // Try to find the first scene in build settings (usually main menu)
        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            string firstScenePath = SceneUtility.GetScenePathByBuildIndex(0);
            if (!string.IsNullOrEmpty(firstScenePath))
            {
                return System.IO.Path.GetFileNameWithoutExtension(firstScenePath);
            }
        }
        
        // If all else fails, stay in current scene
        return null;
    }
    
    /// <summary>
    /// Manually trigger disconnection (can be called from UI)
    /// </summary>
    public void ManualDisconnect()
    {
        Debug.Log("Manual disconnection requested");
        HandleDisconnection("Manual disconnect");
    }
    
    /// <summary>
    /// Return to main menu
    /// </summary>
    public void ReturnToMainMenu()
    {
        HandleDisconnection("Returning to main menu");
    }
}
