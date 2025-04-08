using UnityEngine;
using TMPro;
using System.Collections;

public class TopDownViewInteract : MonoBehaviour
{
    [Header("UI Settings")]
    public TMP_Text interactionText;
    private bool isPlayerNear = false;
    private bool isInTopView = false;

    public GameObject playerCameraObject;
    public GameObject Maincamera;
    public int startCounter = 0;
    public bool inGame = false;

    private void Start()
    {
        if (inGame == true)
        {
            FindPlayerGameObject();
        }
    }


    public void FindPlayerGameObject()
    {
        if (playerCameraObject == null)
        {
            playerCameraObject = GameObject.FindGameObjectWithTag("Player");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            interactionText.text = "Press E to use Roulette";
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (interactionText != null)
                interactionText.gameObject.SetActive(false);

            if (isInTopView)
            {
                isInTopView = false;
            }
        }
    }

    private void Update()
    {
        if (inGame == true && startCounter == 0)
        {
            startCounter++;
            Start();
        }
        else if (inGame == true)
        {
            if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("E pressed");
                ToggleTopDownView();
            }
        }
    }

    private void ToggleTopDownView()
    {
        if (!isInTopView)
        {
            playerCameraObject.SetActive(false);
            Maincamera.SetActive(true);
        }
        else
        {
            playerCameraObject.SetActive(true);
            Maincamera.SetActive(false);
        }

        isInTopView = !isInTopView;
    }

}