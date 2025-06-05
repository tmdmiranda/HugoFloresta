using UnityEngine;
using Unity.Netcode;

public class FlashLightController : NetworkBehaviour
{
    [Header("Flashlight Components")]
    public Light flashlightLight;
    public MeshRenderer flashlightMesh;
      [Header("Network Variables")]
    private NetworkVariable<bool> isOn = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        // Subscribe to network variable changes
        isOn.OnValueChanged += OnFlashlightStateChanged;
        
        // Set initial state
        UpdateFlashlightVisuals(isOn.Value);
    }

    public override void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (isOn != null)
            isOn.OnValueChanged -= OnFlashlightStateChanged;
        
        base.OnDestroy();
    }    void Update()
    {
        // Only the owner can control the flashlight
        if (!IsOwner) return;
        
        if (Input.GetKeyDown(KeyCode.F))
        {
            // If using Server write permission, use ServerRpc
            ToggleFlashlightServerRpc();
            
            // If using Owner write permission, modify directly:
            // isOn.Value = !isOn.Value;
        }
    }

    [ServerRpc]
    private void ToggleFlashlightServerRpc()
    {
        // Toggle the network variable on the server
        isOn.Value = !isOn.Value;
    }

    private void OnFlashlightStateChanged(bool previousValue, bool newValue)
    {
        UpdateFlashlightVisuals(newValue);
    }

    private void UpdateFlashlightVisuals(bool state)
    {
        // Update light component
        if (flashlightLight != null)
            flashlightLight.enabled = state;
            
        // Update mesh renderer
        if (flashlightMesh != null)
            flashlightMesh.enabled = state;
    }
}
