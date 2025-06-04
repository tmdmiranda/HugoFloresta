using UnityEngine;
using TMPro;
using System.Collections;
using Zenject;
public class TopDownViewInteract : MonoBehaviour
{


    [Header("UI Settings")]
    [SerializeField] [Inject] RoletaSystem _roletaSystem; // injecao xddCinema
    public TMP_Text interactionText;
    private bool isPlayerNear = false;
    private bool isInTopView = false;

    public GameObject playerCameraObject;
    public GameObject Maincamera;
    public int startCounter = 0;
    public bool inGame = false;


    private void Start()
    {
       // if (inGame == true)
       // {
       //     FindPlayerGameObject();
       // }

        _roletaSystem = GameObject.Find("Manager").GetComponent<RoletaSystem>();
    }


    public void FindPlayerGameObject()
    {
       // if (playerCameraObject == null)
       // {
           // playerCameraObject = GameObject.FindGameObjectWithTag("Player");
       // }
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
               // ToggleTopDownView();
            }
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            _roletaSystem.SpawnNewRoleta();
        }
    }

    private void ToggleTopDownView()
    {
        if (!isInTopView)
        {
      //      playerCameraObject.SetActive(false);
      //      Maincamera.SetActive(true);
        }
        else
        {
       //     playerCameraObject.SetActive(true);
     //       Maincamera.SetActive(false);
        }

        isInTopView = !isInTopView;
    }



}