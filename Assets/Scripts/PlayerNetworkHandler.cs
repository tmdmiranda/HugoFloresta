using Unity.Netcode;
using UnityEngine;
using Unity.Collections;


public class PlayerNetworkHandler : NetworkBehaviour
{
    public void InitializePlayer(ulong clientId)
    {
        if (IsServer)
        {
            // Sync initialization across network
            InitializeClientRpc(clientId);
        }
    }

    [ClientRpc]
    private void InitializeClientRpc(ulong clientId)
    {
        if (IsOwner)
        {
            // Client-specific initialization
            Debug.Log($"Player {clientId} initialized");
            // Add your player setup code here
        }
    }
}