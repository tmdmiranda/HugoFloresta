using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Collections;

public class LobbyManager : MonoBehaviour
{
    public TMP_Text[] playerNamesText; // Make sure these are assigned in Inspector

    public void UpdatePlayerList(NetworkList<FixedString32Bytes> playerNames)
    {
        for (int i = 0; i < playerNamesText.Length; i++)
        {
            playerNamesText[i].text = "";// Hide all text elements first
            if (i < playerNames.Count)
            {
                playerNamesText[i].text = playerNames[i].ToString();
            }

        }
    }
}