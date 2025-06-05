# Quick Integration Guide for Disconnection System

## 🎯 GOAL: Add 'J' key debug disconnect to your MainScene and StartingMenu

## 📋 Integration Steps

### Option 1: Super Easy Setup (Recommended)

1. **In BOTH scenes (MainScene and StartingMenu):**
   - Create an empty GameObject
   - Name it "DisconnectionSystem"
   - Add the `DisconnectionSystemSetup` script to it
   - That's it! The system will auto-configure everything

2. **Test it:**
   - Run your game
   - Press 'J' key at any time to trigger debug disconnect
   - Watch the console for disconnection logs

### Option 2: Manual Setup (More Control)

1. **In MainScene:**
   - Add `DisconnectionTester` script to any GameObject
   - Add `DisconnectionSceneManager` script to any GameObject
   - In DisconnectionTester inspector, set "Force Disconnect Key" to 'J'
   - In DisconnectionSceneManager, set scene names:
     - Main Menu Scene Name: "StartingMenu"
     - Lobby Scene Name: "StartingMenu"

2. **In StartingMenu:**
   - Add `DisconnectionSystemSetup` script to any GameObject
   - Configure it to only create what you need

## 🎮 How It Works

### When 'J' is pressed:
1. **If in MainScene (game running):**
   - Player disconnects gracefully
   - Network objects cleaned up
   - Scene transitions back to StartingMenu
   - UI shows disconnection reason

2. **If in StartingMenu (lobby):**
   - If connected as host/client, disconnects gracefully
   - If not connected, shows warning in console

## 🔧 Configuration Options

### DisconnectionSystemSetup (Easy Setup)
- `Enable Debug Disconnect`: Turn on/off J key functionality
- `Debug Disconnect Key`: Change from J to any key you want
- `Auto Create Components`: Automatically creates needed components

### DisconnectionTester (Manual Setup)
- `Enable Debug UI`: Shows on-screen debug info
- `Force Disconnect Key`: The key to press for disconnect (set to J)

## 🧪 Testing Scenarios

### Test 1: Host Disconnect
1. Start as Host in StartingMenu
2. Have another player join
3. Start the game (go to MainScene)
4. Press 'J' as host
5. Verify: All players return to StartingMenu

### Test 2: Client Disconnect
1. Join someone else's game
2. Wait until in MainScene
3. Press 'J' as client
4. Verify: You return to StartingMenu, others stay in game

### Test 3: Lobby Disconnect
1. Start hosting in StartingMenu
2. Before starting game, press 'J'
3. Verify: Hosting stops, UI resets

## 🐛 Troubleshooting

### "Nothing happens when I press J"
- Check console for "DEBUG DISCONNECT TRIGGERED" message
- Verify NetworkManager exists in scene
- Make sure you have either P2P_Manager or DisconnectionTester in scene

### "Disconnection works but scene doesn't change"
- Check that "StartingMenu" scene is in Build Settings
- Verify scene names in DisconnectionSceneManager match exactly
- Check console for scene loading errors

### "Game crashes on disconnect"
- This might be normal during development
- Check Unity console for specific error messages
- Verify all NetworkObjects have proper cleanup scripts

## 📁 Files Added/Modified

### New Files:
- `DisconnectionSystemSetup.cs` - Easy integration component
- `DisconnectionTester.cs` - Modified to use J key
- `DisconnectionSceneManager.cs` - Modified with your scene names

### Modified Files:
- `P2P_Manager.cs` - Enhanced disconnection handling
- `LobbyManager.cs` - Added disconnect button support

## 🚀 Quick Start Commands

### Add to MainScene:
1. Create empty GameObject → name it "DisconnectionSystem"
2. Add `DisconnectionSystemSetup` component
3. Done!

### Add to StartingMenu:
1. Create empty GameObject → name it "DisconnectionSystem"  
2. Add `DisconnectionSystemSetup` component
3. Done!

That's it! Press 'J' anywhere to test disconnection.

## 💡 Pro Tips

1. **For Production**: Set `enableDebugDisconnect = false` in DisconnectionSystemSetup
2. **For UI Buttons**: Call `DisconnectionSystemSetup.TestDisconnection()` from button events
3. **For Custom Keys**: Change `debugDisconnectKey` in inspector
4. **For Logging**: Watch Unity Console for detailed disconnection info

## ⚠️ Important Notes

- The 'J' key will work in BOTH scenes automatically
- Debug disconnect only works when actually connected to a network session
- Always test both host and client disconnection scenarios
- The system automatically handles cleanup and scene transitions
