using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.Netcode;

public class RouletteUIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject uiPanel;
    public Button redButton;
    public Button blackButton;
    public Button greenButton;

    public TMP_Text resultColor;
    public TMP_Text resultNumber;

    public TMP_Text pointsText;

    public SpinRouletteManager spinManager;
    public BallLauncher ballLauncher;
    public SpawnRodinhaManager spawnRodinhaManager;

    private string placedBet = "";

    private void Start()
    {
        uiPanel.SetActive(false);

        redButton.onClick.AddListener(() => PlaceBet("Red"));
        blackButton.onClick.AddListener(() => PlaceBet("Black"));
        greenButton.onClick.AddListener(() => PlaceBet("Green"));
    }

    private void PlaceBet(string color)
    {
        placedBet = color;
        StartSpin();
    }

    public void StartSpin()
    {
        spinManager.SpinWheel();
        ballLauncher.LaunchBall();

        redButton.interactable = false;
        blackButton.interactable = false;
        greenButton.interactable = false;

        StartCoroutine(MoveWheelAfterDelay(7f));
    }

    public void RandomResult()
    {
        // Simulate a random result for testing
        string[] colors = { "Red", "Black" };
        string resultColorStr = colors[Random.Range(0, colors.Length)];
        int resultNum = resultColorStr == "Green" ? 0 : Random.Range(1, 37);

        resultColor.text = resultColorStr;
        resultColor.color = resultColorStr switch
        {
            "Red" => Color.red,
            "Black" => Color.black,
            "Green" => Color.green,
            _ => Color.white
        };

        resultNumber.text = $"Number: {resultNum}";

        if (resultColorStr == placedBet)
        {
            Debug.Log($"You won! Color: {resultColorStr}, Number: {resultNum}");
            string text = pointsText.text;
            string numberPart = new string(System.Text.RegularExpressions.Regex.Replace(text, @"[^\d]", "").ToCharArray());
            int currentPoints = int.Parse(numberPart);
            currentPoints += 1;
            pointsText.text = "Points" + currentPoints.ToString();
        }
    }

    private IEnumerator MoveWheelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RandomResult();
        yield return new WaitForSeconds(2f);

        Debug.Log("Moving wheel to new position after delay.");
        spawnRodinhaManager.SpawnRoulette();

        redButton.interactable = true;
        blackButton.interactable = true;
        greenButton.interactable = true;
    }

    public void AnnounceResult(string number, string color)
    {
        // Placeholder if you later want server-side result reporting
        resultColor.text = $"Color: {color}";
        resultColor.color = color switch
        {
            "Red" => Color.red,
            "Black" => Color.black,
            "Green" => Color.green,
            _ => Color.white
        };
        resultNumber.text = $"Number: {number}";
    }

    private void OnTriggerEnter(Collider other)
    {
        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsLocalPlayer) return;

        if (other.CompareTag("Player"))
        {
            uiPanel.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsLocalPlayer) return;

        if (other.CompareTag("Player"))
        {
            uiPanel.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
