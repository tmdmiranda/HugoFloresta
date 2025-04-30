using UnityEngine;
using Unity.Netcode;

public class HostAGame : MonoBehaviour
{
    public GameObject menuPrefab;
    public GameObject canvas;
    public void OnClikHostGame()
    {
        
        if (canvas != null)
        {
           canvas.SetActive(false);
        }

       // NetworkManager.Singleton.StartHost();
    }
}
