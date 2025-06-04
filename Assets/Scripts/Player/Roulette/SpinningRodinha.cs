using UnityEngine;
using System.Collections;

public class RodinhaSpinner : MonoBehaviour
{
    public Transform rodinhaTransform; // assign your "Rodinha" GameObject here
    public AnimationCurve decelerationCurve;
    public float spinDuration = 5f;
    public int[] numbers = new int[37]; // fill with standard roulette order (0–36)

    private bool isSpinning = false;

    public void Spin()
    {
        if (isSpinning) return;
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;

        float anglePerNumber = 360f / numbers.Length;
        int resultIndex = Random.Range(0, numbers.Length);
        int resultNumber = numbers[resultIndex];

        float finalAngle = 360f * 10 + (resultIndex * anglePerNumber); // 10 full spins + target
        float startAngle = rodinhaTransform.eulerAngles.z;
        float currentTime = 0f;

        while (currentTime < spinDuration)
        {
            currentTime += Time.deltaTime;
            float t = currentTime / spinDuration;
            float smooth = decelerationCurve.Evaluate(t);
            float angle = Mathf.Lerp(startAngle, finalAngle, smooth);
            rodinhaTransform.eulerAngles = new Vector3(0, 0, angle);
            yield return null;
        }

        rodinhaTransform.eulerAngles = new Vector3(0, 0, finalAngle);

        int landedNumber = numbers[resultIndex];
        string color = GetColorForNumber(landedNumber);

        Debug.Log($"Rodinha stopped at {landedNumber} ({color})");

        // Pass this to RouletteManager
        if (RouletteManager.Instance.IsServer)
        {
            RouletteManager.Instance.ReceiveResultFromRodinha(landedNumber);
        }

        isSpinning = false;
    }

    private string GetColorForNumber(int number)
    {
        if (number == 0) return "Green";
        int[] reds = {32,19,21,25,34,27,36,30,23,5,16,1,14,9,18,7,12,3};
        return System.Array.Exists(reds, n => n == number) ? "Red" : "Black";
    }
}
