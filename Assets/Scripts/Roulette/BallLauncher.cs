using UnityEngine;

public class BallLauncher : MonoBehaviour
{
    public Rigidbody ballRb;
    public Transform launchPoint;
    public float launchForce = 10f;

    public void LaunchBall()
    {
        ballRb.transform.position = launchPoint.position;
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        Vector3 direction = launchPoint.right + launchPoint.up * 0.3f; // slight upward
        ballRb.AddForce(direction.normalized * launchForce, ForceMode.VelocityChange);
    }
}
