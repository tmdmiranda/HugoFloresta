using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Collections;
using System.Linq; // For the ToArray() conversion

public class LobbyManager : MonoBehaviour
{
    public GameObject[] PlayerGameObjects; // Assign these in the inspector
    public TMP_Text[] nameText; // Reference to the TMP_Text component for displaying player names

    // Alternative if each PlayerGameObject has its own TMP_Text
    public void UpdatePlayerList(NetworkList<FixedString32Bytes> playerNames)
    {
        for (int i = 0; i < PlayerGameObjects.Length; i++)
        {
            bool shouldBeActive = i < playerNames.Count;
            PlayerGameObjects[i].SetActive(shouldBeActive);

            if (shouldBeActive)
            {
                PlayerGameObjects[i].GetComponentInChildren<TMP_Text>().text = playerNames[i].ToString();

            }
        }
    }
}