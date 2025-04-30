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
    public Button[] isReadyButtons;
    public Color readyColor = Color.green;
    public Color waitingColor = Color.yellow;

    [Header("Game Controls")]
    public Button startGameButton;
    public TMP_Text startGameErrorText;

    private P2P_Manager p2pManager;

    private void Start()
    {
        p2pManager = FindObjectOfType<P2P_Manager>();
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
            if (isReadyButtons != null && i < isReadyButtons.Length)
            {
                isReadyButtons[i].gameObject.SetActive(false);
                isReadyButtons[i].onClick.RemoveAllListeners();
            }
        }

        // Then populate active players
        for (int i = 0; i < players.Count && i < playerNameTexts.Length; i++)
        {
            var player = players[i];
            playerNameTexts[i].gameObject.SetActive(true);
            isReadyButtons[i].gameObject.SetActive(true);
            playerReadyTexts[i].gameObject.SetActive(true);
            playerNameTexts[i].text = player.playerName.ToString();

            // Update ready status display for all players
            playerReadyTexts[i].text = player.isReady ? "READY" : "WAITING";
            playerReadyTexts[i].color = player.isReady ? readyColor : waitingColor;

            // Only make the button interactable for this player
            isReadyButtons[i].interactable = (player.clientId == NetworkManager.Singleton.LocalClientId);

            // Add listener only for local player
            if (player.clientId == NetworkManager.Singleton.LocalClientId)
            {
                int index = i; // Capture the current index in a local variable
                isReadyButtons[i].onClick.AddListener(() =>
                {
                    p2pManager.ToggleReadyStatus();
                    // Update the button's visual state immediately
                    playerReadyTexts[index].text = !player.isReady ? "READY" : "WAITING";
                    playerReadyTexts[index].color = !player.isReady ? readyColor : waitingColor;
                });
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

    public void OnHostStatusChanged(bool isHost)
    {
        startGameButton.gameObject.SetActive(isHost);
        startGameErrorText.text = isHost ? "" : "Only host can start game";
    }
}