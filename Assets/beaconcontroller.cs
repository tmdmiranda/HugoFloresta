using UnityEngine;

public class BeaconController : MonoBehaviour
{
    public ParticleSystem beaconParticles;
    public Color beaconColor = new Color(0.2f, 0.6f, 1f, 0.8f);
    public float intensity = 1f;
    
    private ParticleSystem.MainModule mainModule;
    
    void Start()
    {
        mainModule = beaconParticles.main;
        UpdateBeacon();
    }
    
    void UpdateBeacon()
    {
        mainModule.startColor = beaconColor * intensity;
    }
    
    // Call this to change beacon color dynamically
    public void SetBeaconColor(Color newColor)
    {
        beaconColor = newColor;
        UpdateBeacon();
    }
}