using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("Player Displays")]
    public TMP_Text[] playerNameTexts;
    public TMP_Text[] playerReadyTexts;
    public Color readyColor = Color.green;
    public Color waitingColor = Color.yellow;

    [Header("Game Controls")]
    public Button startGameButton;
    public TMP_Text startGameErrorText;

    private void Start()
    {
        // Only show start button for host
        startGameButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);
        startGameButton.onClick.AddListener(OnStartGameClicked);
    }

    public void UpdatePlayerList(NetworkList<P2P_Manager.PlayerLobbyData> players)
    {
        // First hide all slots
        for (int i = 0; i < playerNameTexts.Length; i++)
        {
            playerNameTexts[i].gameObject.SetActive(false);
            if (playerReadyTexts != null && i < playerReadyTexts.Length)
                playerReadyTexts[i].gameObject.SetActive(false);
        }

        // Then populate active players
        for (int i = 0; i < players.Count && i < playerNameTexts.Length; i++)
        {
            var player = players[i];
            playerNameTexts[i].gameObject.SetActive(true);
            playerNameTexts[i].text = player.playerName.ToString();

            if (playerReadyTexts != null && i < playerReadyTexts.Length)
            {
                playerReadyTexts[i].gameObject.SetActive(true);
                playerReadyTexts[i].text = player.isReady ? "READY" : "WAITING";
                playerReadyTexts[i].color = player.isReady ? readyColor : waitingColor;
            }
            else
            {
                playerNameTexts[i].text += player.isReady ? " ✓" : "";
            }
        }

        // Update start button interactability
        if (NetworkManager.Singleton.IsHost)
        {
            UpdateStartButtonState(players);
        }
    }

    private void UpdateStartButtonState(NetworkList<P2P_Manager.PlayerLobbyData> players)
    {
        bool allReady = true;
        bool atLeastTwoPlayers = players.Count >= 2;

        foreach (var player in players)
        {
            if (!player.isReady)
            {
                allReady = false;
                break;
            }
        }

        startGameButton.interactable = allReady && atLeastTwoPlayers;
        
        if (!atLeastTwoPlayers)
        {
            startGameErrorText.text = "Need at least 2 players";
        }
        else if (!allReady)
        {
            startGameErrorText.text = "All players must be ready";
        }
        else
        {
            startGameErrorText.text = "";
        }
    }

    public void OnStartGameClicked()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        // Get reference to P2P_Manager
        P2P_Manager p2pManager = FindObjectOfType<P2P_Manager>();
        if (p2pManager != null)
        {
            p2pManager.StartGame();
        }
        else
        {
            Debug.LogError("P2P_Manager not found in scene!");
            startGameErrorText.text = "System error: Could not start game";
        }
    }

    // Called when host status changes
    public void OnHostStatusChanged(bool isHost)
    {
        startGameButton.gameObject.SetActive(isHost);
        startGameErrorText.text = isHost ? "" : "Only host can start game";
    }
}