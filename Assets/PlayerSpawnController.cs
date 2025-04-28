using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnController : NetworkBehaviour
{
    [Header("Components")]
    public GameObject playerCamera;
    public AudioListener audioListener;
    public CharacterController characterController;

    private void Start()
    {
        // Only enable for local player
        bool isLocalPlayer = IsOwner;
        
        playerCamera.SetActive(isLocalPlayer);
        audioListener.enabled = isLocalPlayer;
    }
}