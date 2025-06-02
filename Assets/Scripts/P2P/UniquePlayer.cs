using Unity.Netcode;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(NetworkObject))]
public class UniquePlayer : NetworkBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private TextMeshPro nameTag;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner)
        {
            ApplyVisualCustomization();
        }
    }

    public void SetPlayerName(string name)
    {
        if (nameTag != null)
        {
            nameTag.text = name;
        }
        else
        {
            Debug.LogWarning("NameTag reference not set in inspector!", this);
        }
    }

    private void ApplyVisualCustomization()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("PlayerDataManager instance not found!", this);
            return;
        }

        var playerData = PlayerDataManager.Instance.GetPlayerData(OwnerClientId);

        // Set name
        SetPlayerName(playerData.playerName);

    }
}