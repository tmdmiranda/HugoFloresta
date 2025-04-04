using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    public GameObject canvas;
    public GameObject joinPrefab;
    public void OnClickDestroyPrefab()
    {
        Debug.Log("Destroying prefab");
        canvas.SetActive(false);
    }

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

}
