using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.Netcode;
using System.Text.RegularExpressions;

public class RouletteUIManager : NetworkBehaviour
{
    [Header("UI")]
    public GameObject uiPanel;
    public Button redButton;
    public Button blackButton;
    public Button greenButton;

    public TMP_Text resultColor;
    public TMP_Text resultNumber;
    public TMP_Text pointsText;

    [Header("Game References")]
    public SpawnRodinhaManager spawnRodinhaManager;

    [Header("Sanity Settings")]
    public float sanityRecoveryAmount = 40f;
    public float rouletteDetectionRadius = 5f;

    [Header("Game Balance")]
    public int redBlackWinPoints = 1;
    public int greenWinPoints = 5; // Higher reward for green (riskier bet)
    
    private string placedBet = "";
    private SanitySystem playerSanitySystem;
    private NetworkObject currentPlayer;
    private GameObject currentRoulette;
    private bool isGameInProgress = false;

    // Roulette number-to-color mapping (European roulette)
    private readonly int[] redNumbers = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };

    private void Start()
    {
        uiPanel.SetActive(false);

        redButton.onClick.AddListener(() => PlaceBet("Red"));
        blackButton.onClick.AddListener(() => PlaceBet("Black"));
        greenButton.onClick.AddListener(() => PlaceBet("Green"));

        InvokeRepeating(nameof(CheckPlayerNearRoulette), 0.5f, 0.5f);
    }

    private void CheckPlayerNearRoulette()
    {
        GameObject roulette = GameObject.FindWithTag("Roulette");
        
        if (roulette == null)
        {
            if (uiPanel.activeSelf)
            {
                HideUI();
            }
            return;
        }

        NetworkObject localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;

        float distance = Vector3.Distance(localPlayer.transform.position, roulette.transform.position);

        if (distance <= rouletteDetectionRadius)
        {
            if (!uiPanel.activeSelf && !isGameInProgress)
            {
                ShowUI(localPlayer);
                currentRoulette = roulette;
            }
        }
        else
        {
            if (uiPanel.activeSelf)
            {
                HideUI();
            }
        }
    }

    private NetworkObject GetLocalPlayer()
    {
        if (!NetworkManager.Singleton.IsClient) return null;

        NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
        foreach (NetworkObject netObj in networkObjects)
        {
            if (netObj.IsLocalPlayer && netObj.CompareTag("Player"))
            {
                return netObj;
            }
        }
        return null;
    }

    private void ShowUI(NetworkObject player)
    {
        currentPlayer = player;
        playerSanitySystem = player.GetComponentInChildren<SanitySystem>();
        
        if (playerSanitySystem == null)
        {
            Debug.LogWarning("SanitySystem not found on player!");
        }

        uiPanel.SetActive(true);
        
        // Only modify cursor if this is the local player
        if (player.IsLocalPlayer)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void HideUI()
    {
        currentPlayer = null;
        playerSanitySystem = null;
        currentRoulette = null;

        uiPanel.SetActive(false);
        
        // Reset cursor for local player
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void PlaceBet(string color)
    {
        if (isGameInProgress) return;
        
        placedBet = color;
        StartSpin();
    }

    public void StartSpin()
    {
        if (isGameInProgress) return;
        
        isGameInProgress = true;

        // Restore sanity immediately when playing (participation reward)
        if (playerSanitySystem != null)
        {
            playerSanitySystem.RestoreSanity(sanityRecoveryAmount * 0.5f); // Half now, half if win
        }

        // Sync game state across network
        if (IsServer)
        {
            StartSpinServerRpc();
        }
        else
        {
            StartSpinServerRpc();
        }

        SetButtonsInteractable(false);
        StartCoroutine(GameSequence());
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartSpinServerRpc()
    {
        // Trigger spin effects on all clients
        TriggerSpinEffectsClientRpc();
    }

    [ClientRpc]
    private void TriggerSpinEffectsClientRpc()
    {
        if (currentRoulette != null)
        {
            SpinRouletteManager spinManager = currentRoulette.GetComponent<SpinRouletteManager>();
            BallLauncher ballLauncher = currentRoulette.GetComponent<BallLauncher>();

            if (spinManager != null) spinManager.SpinWheel();
            if (ballLauncher != null) ballLauncher.LaunchBall();
        }
    }

    private IEnumerator GameSequence()
    {
        // Wait for spin animation
        yield return new WaitForSeconds(7f);
        
        // Generate and display result
        GenerateResult();
        
        // Show result for a moment
        yield return new WaitForSeconds(2f);

        // Move roulette and reset
        Debug.Log("Moving wheel to new position after delay.");
        if (spawnRodinhaManager != null)
        {
            spawnRodinhaManager.SpawnRoulette();
        }

        SetButtonsInteractable(true);
        isGameInProgress = false;
        placedBet = "";
    }

    private void GenerateResult()
    {
        // Generate realistic roulette result
        int resultNum = Random.Range(0, 37); // 0-36 for European roulette
        string resultColorStr = GetColorFromNumber(resultNum);

        DisplayResult(resultColorStr, resultNum);
        ProcessWinLoss(resultColorStr);
    }

    private string GetColorFromNumber(int number)
    {
        if (number == 0) return "Green";
        
        // Check if number is in red numbers array
        foreach (int redNum in redNumbers)
        {
            if (number == redNum) return "Red";
        }
        
        return "Black";
    }

    private void DisplayResult(string color, int number)
    {
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

    private void ProcessWinLoss(string resultColorStr)
    {
        if (resultColorStr == placedBet)
        {
            Debug.Log($"You won! Color: {resultColorStr}");
            
            // Award points based on bet type
            int pointsWon = placedBet == "Green" ? greenWinPoints : redBlackWinPoints;
            UpdatePoints(pointsWon);
            
            // Additional sanity reward for winning
            if (playerSanitySystem != null)
            {
                playerSanitySystem.RestoreSanity(sanityRecoveryAmount * 0.5f);
                Debug.Log($"Player won and recovered additional {sanityRecoveryAmount * 0.5f} sanity! Current sanity: {playerSanitySystem.GetSanity()}");
            }
        }
        else
        {
            Debug.Log($"You lost. Result: {resultColorStr}, Your bet: {placedBet}");
        }
    }

    private void UpdatePoints(int pointsToAdd)
    {
        if (pointsText == null) return;

        string text = pointsText.text;
        string numberPart = Regex.Replace(text, @"[^\d]", "");
        
        if (int.TryParse(numberPart, out int currentPoints))
        {
            currentPoints += pointsToAdd;
            pointsText.text = $"Points: {currentPoints}";
        }
        else
        {
            Debug.LogWarning("Could not parse points from text: " + text);
            pointsText.text = $"Points: {pointsToAdd}";
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        redButton.interactable = interactable;
        blackButton.interactable = interactable;
        greenButton.interactable = interactable;
    }

    // Public method for external result announcements (if needed)
    public void AnnounceResult(string number, string color)
    {
        if (int.TryParse(number, out int num))
        {
            DisplayResult(color, num);
        }
        else
        {
            DisplayResult(color, 0);
        }
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }
}