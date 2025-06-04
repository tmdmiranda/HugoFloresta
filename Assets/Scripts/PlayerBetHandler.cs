using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerBetHandler : NetworkBehaviour
{
    private NetworkVariable<int> totalPoints = new NetworkVariable<int>(0);
    private string currentBet = "";

    public TextMeshProUGUI pointsText;

    public void BetColor(string color)
    {
        currentBet = color;
        if (IsOwner)
        {
            RouletteManager.Instance.PlaceBet(color);
        }
    }

    [ClientRpc]
    public void AddPointClientRpc()
    {
        totalPoints.Value++;
    }

    private void Update()
    {
        if (IsOwner && pointsText != null)
        {
            pointsText.text = "Total Points: " + totalPoints.Value;
        }
    }
}
