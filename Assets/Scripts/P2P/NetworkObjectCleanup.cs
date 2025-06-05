using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles proper cleanup for network objects during disconnection
/// Attach this to player prefabs and other important network objects
/// </summary>
public class NetworkObjectCleanup : NetworkBehaviour
{
    [Header("Cleanup Settings")]
    [SerializeField] private bool cleanupOnDisconnect = true;
    [SerializeField] private bool notifyOtherClients = true;
    
    [Header("References")]
    [SerializeField] private GameObject[] objectsToCleanup;
    [SerializeField] private Component[] componentsToDisable;
    
    private bool isCleaningUp = false;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"NetworkObjectCleanup spawned for object: {gameObject.name}");
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (cleanupOnDisconnect && !isCleaningUp)
        {
            PerformCleanup();
        }
    }
    
    /// <summary>
    /// Perform cleanup operations
    /// </summary>
    public void PerformCleanup()
    {
        if (isCleaningUp) return;
        
        isCleaningUp = true;
        Debug.Log($"Performing cleanup for network object: {gameObject.name}");
        
        // Disable components first
        DisableComponents();
        
        // Clean up referenced objects
        CleanupReferencedObjects();
        
        // Notify other clients if needed
        if (notifyOtherClients && IsSpawned && IsServer)
        {
            NotifyCleanupClientRpc(NetworkObject.NetworkObjectId);
        }
        
        // Final cleanup
        FinalCleanup();
    }
    
    /// <summary>
    /// Disable specified components
    /// </summary>
    private void DisableComponents()
    {
        if (componentsToDisable != null)
        {
            foreach (var component in componentsToDisable)
            {
                if (component != null)
                {
                    try
                    {
                        if (component is Behaviour behaviour)
                        {
                            behaviour.enabled = false;
                        }
                        else if (component is Collider collider)
                        {
                            collider.enabled = false;
                        }
                        else if (component is Renderer renderer)
                        {
                            renderer.enabled = false;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Error disabling component {component.GetType()}: {ex.Message}");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Clean up referenced objects
    /// </summary>
    private void CleanupReferencedObjects()
    {
        if (objectsToCleanup != null)
        {
            foreach (var obj in objectsToCleanup)
            {
                if (obj != null)
                {
                    try
                    {
                        // If it's a network object, despawn it properly
                        if (obj.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned && IsServer)
                        {
                            netObj.Despawn();
                        }
                        else
                        {
                            Destroy(obj);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Error cleaning up object {obj.name}: {ex.Message}");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Notify other clients about cleanup
    /// </summary>
    [ClientRpc]
    private void NotifyCleanupClientRpc(ulong networkObjectId)
    {
        Debug.Log($"Received cleanup notification for network object ID: {networkObjectId}");
        
        // Additional client-side cleanup if needed
        HandleClientSideCleanup();
    }
    
    /// <summary>
    /// Handle any client-specific cleanup
    /// </summary>
    private void HandleClientSideCleanup()
    {
        // Remove from any local tracking collections
        if (P2P_Manager.Instance != null)
        {
            // The P2P_Manager will handle removing from playerObjects dictionary
        }
        
        // Handle UI updates
        UpdateUIAfterCleanup();
    }
    
    /// <summary>
    /// Update UI elements after cleanup
    /// </summary>
    private void UpdateUIAfterCleanup()
    {
        // Find and update lobby UI if present
        var lobbyManager = FindFirstObjectByType<LobbyManager>();
        if (lobbyManager != null)
        {
            // The lobby will be updated through the normal network list events
        }
    }
    
    /// <summary>
    /// Final cleanup operations
    /// </summary>
    private void FinalCleanup()
    {
        // Disable any remaining components on this object
        var components = GetComponentsInChildren<MonoBehaviour>();
        foreach (var comp in components)
        {
            if (comp != this && comp != null)
            {
                try
                {
                    comp.enabled = false;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Error disabling component {comp.GetType()}: {ex.Message}");
                }
            }
        }
    }
    
    /// <summary>
    /// Force cleanup (can be called manually)
    /// </summary>
    public void ForceCleanup()
    {
        PerformCleanup();
    }
    
    /// <summary>
    /// Add objects to cleanup list at runtime
    /// </summary>
    public void AddObjectToCleanup(GameObject obj)
    {
        if (obj != null)
        {
            System.Array.Resize(ref objectsToCleanup, objectsToCleanup.Length + 1);
            objectsToCleanup[objectsToCleanup.Length - 1] = obj;
        }
    }
    
    /// <summary>
    /// Add component to disable list at runtime
    /// </summary>
    public void AddComponentToDisable(Component component)
    {
        if (component != null)
        {
            System.Array.Resize(ref componentsToDisable, componentsToDisable.Length + 1);
            componentsToDisable[componentsToDisable.Length - 1] = component;
        }
    }
}
