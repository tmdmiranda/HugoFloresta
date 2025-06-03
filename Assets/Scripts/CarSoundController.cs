using UnityEngine;

[RequireComponent(typeof(AudioSource), typeof(Rigidbody))]
public class CarSoundController : MonoBehaviour
{
    public float minSpeed = 0.3f;
    public float maxSpeed = 40f;
    public float minPitch = 0.2f;
    public float maxPitch = 2.0f;

    private Rigidbody rb;
    private AudioSource carSound;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        carSound = GetComponent<AudioSource>();

        // Configure AudioSource for 3D sound
        carSound.spatialBlend = 1f; // Fully 3D
        carSound.rolloffMode = AudioRolloffMode.Logarithmic;
        carSound.minDistance = 5f;
        carSound.maxDistance = 20f;
        carSound.loop = true;
        carSound.playOnAwake = true;

        if (!carSound.isPlaying)
            carSound.Play();
    }

    void Update()
    {
        UpdateEngineSound();
    }

    void UpdateEngineSound()
    {
        float speed = rb.linearVelocity.magnitude;

        float pitch = Mathf.Lerp(minPitch, maxPitch, Mathf.InverseLerp(minSpeed, maxSpeed, speed));
        carSound.pitch = pitch;
    }
}
