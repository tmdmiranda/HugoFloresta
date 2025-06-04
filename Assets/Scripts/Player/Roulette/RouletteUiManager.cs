using UnityEngine;
using UnityEngine.UI;

public class RouletteUIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject uiPanel;
    public Button redButton;
    public Button blackButton;
    public Button greenButton;

    private void Start()
    {
        uiPanel.SetActive(false);

        redButton.onClick.AddListener(() => PlaceBet("Red"));
        blackButton.onClick.AddListener(() => PlaceBet("Black"));
        greenButton.onClick.AddListener(() => PlaceBet("Green"));
    }

    private void PlaceBet(string color)
    {
        Debug.Log("Placed bet on: " + color);
        // Save bet somewhere (e.g., player script or game manager)
        uiPanel.SetActive(false);

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            uiPanel.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            uiPanel.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
