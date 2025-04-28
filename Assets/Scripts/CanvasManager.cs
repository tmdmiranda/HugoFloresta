using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    public GameObject joinPrefab;
    public GameObject lobbyPrefab;

    public GameObject canvas;
    public GameObject startingMenuPrefab;
    public void OnClickActivateJoin()
    {
        joinPrefab.SetActive(true);
    }

    void Update()
    {
        if (canvas.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void InstantiateLobbyPrefab()
    {
        Destroy(startingMenuPrefab);
        Instantiate(lobbyPrefab, Vector3.zero, Quaternion.identity);

    }



}
