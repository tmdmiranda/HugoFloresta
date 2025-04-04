using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform cameraPosition;
    private GameObject Player;

    private void Start()
    {

    }
    private void Update()
    {
        transform.position = cameraPosition.position;
        
    }       

    
}
