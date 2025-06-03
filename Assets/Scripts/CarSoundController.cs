using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSoundController : MonoBehaviour
{
    public float minSpeed = 0.3f;
    public float maxSpeed = 40f;
    private float currentSpeed;

    private Rigidbody rb;
    private AudioSource carSound;
    private CarController carController;

    public float minPitch = 0.2f;
    public float maxPitch = 41.0f;
    private float pitchFromCar;

    void Start()
    {
        carSound = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        EngineSound();   
    }

    void EngineSound()
    {
        currentSpeed = rb.linearVelocity.magnitude;
        pitchFromCar = rb.linearVelocity.magnitude / 50f;

        if (currentSpeed < minSpeed)
        {
            carSound.pitch = minPitch;
        }
        if (currentSpeed > minSpeed && currentSpeed < maxSpeed)
        {
            carSound.pitch = minPitch + pitchFromCar;
        }
        if (currentSpeed > maxSpeed)
        {
            carSound.pitch = maxPitch;
        }
    }
}