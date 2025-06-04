using UnityEngine;

public class SpinRouletteManager : MonoBehaviour
{
    [Header("Wheel & Ball")]
    public Transform wheel;
    public GameObject ballPrefab;
    public Transform ballSpawnPoint;

    private GameObject currentBall;

    [Header("Physics Spin Settings")]
    public float wheelSpinSpeed = 500f;
    public float ballForce = 5f;
    public float ballTorque = 10f;

    [Header("Number Segments")]
    public RouletteSegment[] segments; // You define this array with ranges and colors

    private bool hasSpun = false;

    public void Spin()
    {
        if (hasSpun) return;

        // Spin wheel
        wheel.GetComponent<Rigidbody>().angularVelocity = new Vector3(0, wheelSpinSpeed, 0);

        // Spawn ball
        currentBall = Instantiate(ballPrefab, ballSpawnPoint.position, Quaternion.identity);
        Rigidbody rb = currentBall.GetComponent<Rigidbody>();
        rb.AddForce(ballSpawnPoint.forward * ballForce, ForceMode.Impulse);
        rb.AddTorque(Random.onUnitSphere * ballTorque);

        hasSpun = true;

        Invoke(nameof(EvaluateResult), 8f); // wait for ball to settle
    }

    private void EvaluateResult()
    {
        if (!currentBall) return;

        float angle = currentBall.transform.eulerAngles.y;
        int number = GetNumberFromAngle(angle);

        RouletteSegment hitSegment = segments[number];
        Debug.Log($"Ball landed on: {hitSegment.number} ({hitSegment.color})");

        // TODO: reward logic here, send result to RouletteManager or players
    }

    private int GetNumberFromAngle(float angle)
    {
        float segmentSize = 360f / segments.Length;
        int index = Mathf.FloorToInt(angle / segmentSize);
        return Mathf.Clamp(index, 0, segments.Length - 1);
    }

    [System.Serializable]
    public class RouletteSegment
    {
        public int number;
        public string color; // "Red", "Black", or "Green"
    }
}
