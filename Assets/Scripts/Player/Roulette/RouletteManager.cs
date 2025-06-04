using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

public class RouletteManager : NetworkBehaviour
{
    public static RouletteManager Instance;

    [SerializeField] private float spinDelay = 3f;

    private NetworkVariable<int> resultNumber = new NetworkVariable<int>();
    private NetworkVariable<string> resultColor = new NetworkVariable<string>();

    private Dictionary<ulong, string> playerBets = new();

    private readonly int[] standardNumbers = new int[]
    {
        0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36,
        11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31, 9,
        22, 18, 29, 7, 28, 12, 35, 3, 26
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ReceiveResultFromRodinha(int number)
    {
        string color = GetColor(number);
        Debug.Log($"Received final number from Rodinha: {number} ({color})");

        resultNumber.Value = number;
        resultColor.Value = color;

        DistributePoints(color);
    }


    public void PlaceBet(string color)
    {
        if (!IsClient || !IsOwner) return;
        SubmitBetServerRpc(color);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitBetServerRpc(string color, ServerRpcParams rpcParams = default)
    {
        playerBets[rpcParams.Receive.SenderClientId] = color;
    }

    public void Spin()
    {
        if (!IsServer) return;
        StartCoroutine(SpinCoroutine());
    }

    private System.Collections.IEnumerator SpinCoroutine()
    {
        yield return new WaitForSeconds(spinDelay);

        int number = standardNumbers[UnityEngine.Random.Range(0, standardNumbers.Length)];
        string color = GetColor(number);

        resultNumber.Value = number;
        resultColor.Value = color;

        DistributePoints(color);
        playerBets.Clear();
    }

    private string GetColor(int number)
    {
        if (number == 0) return "Green";

        // Use standard red/black roulette logic
        int[] red = { 32, 19, 21, 25, 34, 27, 36, 30, 23, 5, 16, 1, 14, 9, 18, 7, 12, 3 };
        return Array.Exists(red, n => n == number) ? "Red" : "Black";
    }

    private void DistributePoints(string resultColor)
    {
        foreach (var kvp in playerBets)
        {
            ulong clientId = kvp.Key;
            string bet = kvp.Value;

            if (bet == resultColor)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                {
                    var handler = client.PlayerObject.GetComponent<PlayerBetHandler>();
                    handler.AddPointClientRpc();
                }
            }
        }
    }

    public int GetCurrentResult() => resultNumber.Value;
    public string GetCurrentColor() => resultColor.Value;
}
