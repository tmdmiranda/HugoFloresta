using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;
    private GameObject Player;

    public Transform orientation;

    float xRotation;
    float yRotation;



    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;   
    }

    private void Update()
    {
        //mouse inputs
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX ;    
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY ;

        yRotation += mouseX;
        xRotation -= mouseY;

        //Limit camera
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);

    }
}
