using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // Import UnityTransport namespace

public class JoinAGame : MonoBehaviour
{
    public InputField ipInputField;  // The InputField where the user types the host IP address
    public Button joinButton;        // The button to trigger the joining process

    private void Start()
    {
        // Add a listener to the join button to call the JoinGame method when clicked
        joinButton.onClick.AddListener(JoinGame);
    }

    public void JoinGame()
    {
        // Get the IP address entered by the player
        string ipAddress = ipInputField.text;

        // Make sure the IP address is not empty
        if (string.IsNullOrEmpty(ipAddress))
        {
            Debug.LogError("IP Address is empty!");
            return;
        }

        // Get the UnityTransport component attached to NetworkManager
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Set the ConnectionData.Address to the entered IP address
        transport.ConnectionData.Address = ipAddress;

        // Optionally, set the port (default is usually 7777 if you're not changing it)
        transport.ConnectionData.Port = 7777;

        // Start the client to connect to the host
        NetworkManager.Singleton.StartClient();

        Debug.Log("Attempting to join the game at IP: " + ipAddress);
    }
}
