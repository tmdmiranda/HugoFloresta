using UnityEngine;
using UnityEngine.UI;

public class WheelSegmentDetector : MonoBehaviour
{
    public SpinRouletteManager spinManager;
    public BallLauncher ballLauncher;
    public RouletteUIManager uiManager;

    // Roulette wheel configuration (adjust based on your wheel)
    private readonly string[] wheelSegments = {
        "0", "32", "15", "19", "4", "21", "2", "25", "17", "34", "6",
        "27", "13", "36", "11", "30", "8", "23", "10", "5", "24", "16",
        "33", "1", "20", "14", "31", "9", "22", "18", "29", "7", "28",
        "12", "35", "3", "26"
    };

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == ballLauncher.ballRb.gameObject && !spinManager.IsSpinning)
        {
            DetectWinningSegment();
        }
    }

    private void DetectWinningSegment()
    {
        // Get the wheel's current angle (normalized to 0-360)
        float wheelAngle = spinManager.CurrentAngle % 360;
        if (wheelAngle < 0) wheelAngle += 360;

        // Calculate which segment the ball landed in
        float segmentSize = 360f / wheelSegments.Length;
        int segmentIndex = Mathf.FloorToInt(wheelAngle / segmentSize);

        // Get the winning number
        string winningNumber = wheelSegments[segmentIndex];

        // Determine the color (simplified - adjust based on your wheel)
        string winningColor = GetColorForNumber(winningNumber);

        Debug.Log($"Landed on: {winningNumber} ({winningColor})");

        // You could now notify the UI manager or other systems
        uiManager.AnnounceResult(winningNumber, winningColor);
    }

    private string GetColorForNumber(string number)
    {
        if (number == "0") return "Green";

        int num = int.Parse(number);
        if ((num >= 1 && num <= 10) || (num >= 19 && num <= 28))
        {
            return num % 2 == 0 ? "Black" : "Red";
        }
        else
        {
            return num % 2 == 0 ? "Red" : "Black";
        }
    }
}