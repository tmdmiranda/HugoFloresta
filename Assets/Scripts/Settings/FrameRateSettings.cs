using UnityEngine;

public class FrameRateSettings : MonoBehaviour
{
    void Start()
    {
        Application.targetFrameRate = -1; // Uncapped FPS
        QualitySettings.vSyncCount = 0; // Disable VSync
    }
}