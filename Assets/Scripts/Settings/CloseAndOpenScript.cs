using UnityEngine;

public class CloseAndOpenScript : MonoBehaviour
{
    public GameObject canvasObject;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (canvasObject != null)
            {
                // Toggle canvas active state
                canvasObject.SetActive(!canvasObject.activeSelf);

                // Show cursor only if canvas is active, otherwise hide it
                if (canvasObject.activeSelf)
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
    }
}
