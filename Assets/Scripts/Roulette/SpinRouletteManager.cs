using UnityEngine;

public class SpinRouletteManager : MonoBehaviour
{
    public Transform wheel;
    public float spinSpeed = 360f; // degrees per second
    private float currentSpeed = 0f;
    private bool isSpinning = false;

    // Add these properties
    public float CurrentAngle => wheel.localEulerAngles.y;
    public bool IsSpinning => isSpinning;

    public void SpinWheel()
    {
        currentSpeed = spinSpeed;
        isSpinning = true;
    }

    void Update()
    {
        if (isSpinning)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * 0.5f); // slow down
            wheel.Rotate(Vector3.up, currentSpeed * Time.deltaTime);

            if (currentSpeed < 5f) // stop condition
            {
                isSpinning = false;
                currentSpeed = 0f;
            }
        }
    }
}
