# Unity Networking Disconnection System

This document describes the comprehensive disconnection system implemented for the Unity project using Netcode for GameObjects.

## Overview

The disconnection system provides smooth and graceful handling of network disconnections in both host and client scenarios, ensuring proper cleanup of network objects, UI states, and scene transitions.

## Components

### 1. P2P_Manager.cs
**Main networking manager with enhanced disconnection handling**

#### Key Features:
- **Comprehensive OnClientDisconnected Handler**: Differentiates between host and client disconnections
- **Automatic Cleanup**: Properly despawns network objects and clears references
- **Scene Transition Management**: Smooth transitions back to lobby/main menu
- **Manual Disconnect Support**: UI-triggered disconnection with proper cleanup

#### Key Methods:
- `OnClientDisconnected(ulong clientId)`: Main disconnection handler
- `HandleClientDisconnection()`: Handles client-specific disconnection
- `HandleHostDisconnection()`: Handles host disconnection scenarios
- `CleanupAndReturnToMenu(string reason)`: Comprehensive cleanup coroutine
- `CleanupNetworking()`: Network-specific cleanup
- `ManualDisconnect()`: User-initiated disconnection

### 2. LobbyManager.cs
**UI management with disconnection support**

#### Key Features:
- **Disconnect Button Integration**: UI button for manual disconnection
- **Disconnection Event Handling**: Responds to disconnection events from P2P_Manager
- **Player List Cleanup**: Clears player displays on disconnection
- **Status Updates**: Shows disconnection reasons to users

#### Key Methods:
- `OnDisconnectClicked()`: Handles disconnect button clicks
- `OnDisconnected(string reason)`: Handles disconnection notifications
- `ClearPlayerList()`: Clears UI player displays

### 3. DisconnectionSceneManager.cs
**Advanced scene transition management (Optional)**

#### Key Features:
- **Smooth Scene Transitions**: Handles scene changes with proper loading screens
- **Customizable Delays**: Configurable transition timing
- **UI Overlay Support**: Optional disconnection overlay messages
- **Multiple Scene Support**: Handles different scene transition scenarios

#### Key Methods:
- `HandleDisconnection(string reason)`: Main entry point for disconnection handling
- `TransitionToScene(string sceneName)`: Scene transition with loading
- `ShowDisconnectionMessage(string message)`: Display disconnection UI

### 4. NetworkObjectCleanup.cs
**Network object lifecycle management**

#### Key Features:
- **Automatic Cleanup**: OnDestroy and OnNetworkDespawn overrides
- **Resource Management**: Proper disposal of network resources
- **Event Unsubscription**: Prevents memory leaks

### 5. DisconnectionTester.cs
**Testing and validation utility**

#### Key Features:
- **Debug UI**: In-game testing interface
- **Multiple Test Scenarios**: Manual, host loss, and network loss simulation
- **Keyboard Shortcuts**: Quick testing with F9, F10, F11 keys
- **System Validation**: Checks if all components are properly configured

## Usage Guide

### Basic Setup

1. **Ensure P2P_Manager is in your scene** with all required references set
2. **Add LobbyManager** to your lobby UI with disconnect button assigned
3. **Optional**: Add DisconnectionSceneManager for advanced scene transitions
4. **Optional**: Add DisconnectionTester for testing and debugging

### Manual Disconnection

```csharp
// From UI button
public void OnDisconnectButtonPressed()
{
    P2P_Manager.Instance.ManualDisconnect();
}

// Direct call
if (P2P_Manager.Instance != null)
{
    P2P_Manager.Instance.ManualDisconnect();
}
```

### Handling Disconnection Events

```csharp
// In your UI manager
public void OnDisconnected(string reason)
{
    Debug.Log($"Disconnected: {reason}");
    // Update UI, show message, etc.
}
```

## Disconnection Scenarios

### 1. Client Disconnection
- **Trigger**: Client loses connection or manually disconnects
- **Behavior**: 
  - Client cleans up local state
  - Returns to main menu/lobby
  - Server removes client from player list
  - Other clients are notified

### 2. Host Disconnection
- **Trigger**: Host shuts down or loses connection
- **Behavior**:
  - All clients detect host loss
  - Clients clean up and return to main menu
  - Network session ends
  - All network objects are cleaned up

### 3. Manual Disconnection
- **Trigger**: User clicks disconnect button
- **Behavior**:
  - Graceful shutdown initiated
  - Network cleanup performed
  - UI updated with status
  - Scene transition to lobby/menu

## Configuration

### P2P_Manager Settings
```csharp
[Header("Connection Settings")]
public int MaxConnections = 8;
public ushort port = 25000;

[Header("UI Elements")]
public TMP_Text connectionStatusText;
public GameObject LobbyPanelPrefab;
```

### DisconnectionSceneManager Settings
```csharp
[Header("Scene Settings")]
[SerializeField] private string mainMenuSceneName = "MainMenu";
[SerializeField] private string lobbySceneName = "Lobby";
[SerializeField] private float transitionDelay = 1f;
```

### LobbyManager Settings
```csharp
[Header("Connection Controls")]
public Button disconnectButton;
```

## Testing

### Using DisconnectionTester

1. **Add DisconnectionTester** to any GameObject in your scene
2. **Enable Debug UI** in inspector
3. **Use keyboard shortcuts**:
   - `F9`: Force disconnect
   - `F10`: Simulate host loss
   - `F11`: Simulate network loss

### Manual Testing

1. **Start as Host**: Test host disconnection scenarios
2. **Join as Client**: Test client disconnection scenarios
3. **Test UI**: Verify disconnect buttons work properly
4. **Test Scene Transitions**: Ensure smooth returns to lobby

## Error Handling

The system includes comprehensive error handling for:
- **Network timeouts**
- **Unexpected disconnections**
- **Scene loading failures**
- **UI reference errors**
- **Network object cleanup issues**

## Best Practices

1. **Always check NetworkManager.Singleton** before network operations
2. **Use try-catch blocks** around network cleanup code
3. **Unsubscribe from events** in OnDestroy methods
4. **Clear UI references** during disconnection
5. **Test all disconnection scenarios** during development

## Troubleshooting

### Common Issues

1. **"NetworkManager not found"**
   - Ensure NetworkManager exists in scene
   - Check initialization order

2. **"Objects not cleaning up"**
   - Verify OnNetworkDespawn implementations
   - Check NetworkObjectCleanup usage

3. **"UI not updating"**
   - Verify LobbyManager references
   - Check event subscriptions

4. **"Scene not transitioning"**
   - Verify scene names in build settings
   - Check DisconnectionSceneManager configuration

### Debug Information

Enable detailed logging by setting debug flags in:
- P2P_Manager: Set debug breakpoints in OnClientDisconnected
- DisconnectionTester: Enable debug UI for real-time information
- Unity Console: Watch for disconnection-related log messages

## Integration Checklist

- [ ] P2P_Manager added to scene with proper references
- [ ] LobbyManager configured with disconnect button
- [ ] NetworkObjectCleanup added to network prefabs
- [ ] DisconnectionSceneManager added (optional)
- [ ] DisconnectionTester added for testing (optional)
- [ ] Scene transitions configured
- [ ] UI references properly set
- [ ] Testing completed for all scenarios

## Version History

- **v1.0**: Initial implementation with basic disconnection handling
- **v1.1**: Added comprehensive cleanup and scene management
- **v1.2**: Added testing utilities and improved error handling
- **v1.3**: Enhanced UI integration and manual disconnect features

---

For additional support or feature requests, refer to the project documentation or contact the development team.
