using UnityEngine;
using System.Collections.Generic;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    private Dictionary<ulong, PlayerData> players = new Dictionary<ulong, PlayerData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterPlayer(ulong clientId, string playerName)
    {
        if (!players.ContainsKey(clientId))
        {
            players[clientId] = new PlayerData
            {
                clientId = clientId,
                playerName = playerName,
            };
        }
    }

    public bool TryGetPlayerName(ulong clientId, out string playerName)
    {
        if (players.TryGetValue(clientId, out PlayerData data))
        {
            playerName = data.playerName;
            return true;
        }
        
        playerName = $"Player{clientId}";
        return false;
    }

    public PlayerData GetPlayerData(ulong clientId)
    {
        if (players.TryGetValue(clientId, out PlayerData data))
        {
            return data;
        }
        
        // Return default data if player not found
        return new PlayerData
        {
            clientId = clientId,
            playerName = $"Player{clientId}",
        };
    }


}

[System.Serializable]
public struct PlayerData
{
    public ulong clientId;
    public string playerName;

}