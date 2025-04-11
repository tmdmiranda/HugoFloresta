using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Collections;

public class LobbyManager : MonoBehaviour
{
    public GameObject[] PlayerGameObjects; // Make sure these are assigned in Inspector

    public void UpdatePlayerList(NetworkList<FixedString32Bytes> playerNames)
    {
        if (PlayerGameObjects == null || PlayerGameObjects.Length == 0)
        {
            Debug.LogError("PlayerGameObjects array is not assigned or empty!");
            return;
        }

        Debug.Log($"Updating player list with {playerNames.Count} players");

        // First disable all player slots
        for (int i = 0; i < PlayerGameObjects.Length; i++)
        {
            if (PlayerGameObjects[i] != null)
            {
                PlayerGameObjects[i].SetActive(false);
            }
        }

        // Activate and update only the needed slots
        for (int i = 0; i < playerNames.Count; i++)
        {
            // Safety check for array bounds
            if (i >= PlayerGameObjects.Length)
            {
                Debug.LogWarning($"Not enough PlayerGameObjects to display all players! Need {playerNames.Count} but only have {PlayerGameObjects.Length}");
                break;
            }

            if (PlayerGameObjects[i] == null)
            {
                Debug.LogError($"PlayerGameObjects[{i}] is null!");
                continue;
            }

            PlayerGameObjects[i].SetActive(true);
            Debug.Log($"Activated player slot {i}");

            // Get the TMP_Text component
            TMP_Text textComponent = PlayerGameObjects[i].GetComponentInChildren<TMP_Text>(true);
            
            if (textComponent != null)
            {
                textComponent.text = playerNames[i].ToString();
                Debug.Log($"Set player {i} name to: {playerNames[i]}");
            }
            else
            {
                Debug.LogError($"No TMP_Text component found on player slot {i} or its children!");
            }
        }
    }
}