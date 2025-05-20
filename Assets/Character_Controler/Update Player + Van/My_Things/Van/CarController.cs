using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : NetworkBehaviour
{
    private float horizontalInput, verticalInput;
    private float currentSteerAngle, currentbreakForce;
    private bool isBreaking;

    private bool isColiding;
    public bool playerInsideCar = false;

    public GameObject player;

    private CarInputHandler carInputHandler;

    [Header("Drive Settings")]
    private bool isTransiting;
    [SerializeField] public Transform driverSeat;
    [SerializeField] public Transform exitPoint;
    private float transitionSpeed = 0.1f;
    [SerializeField] public Transform carCamera;
    [SerializeField] public Transform playerCameraY;
    [SerializeField] public Transform playerCameraX;

    [Header("Car Settings")]
    [SerializeField] private float motorForce;
    [SerializeField] private float breakForce;
    [SerializeField] private float maxSteerAngle;

    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider frontLeftWheelCollider;
    [SerializeField] private WheelCollider frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider;
    [SerializeField] private WheelCollider rearRightWheelCollider;

    [Header("Wheels")]
    [SerializeField] private Transform frontLeftWheelTransform;
    [SerializeField] private Transform frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform;
    [SerializeField] private Transform rearRightWheelTransform;

    [Header("Steering Wheel")]
    [SerializeField] private Transform steeringWheel;
    [SerializeField] private float steeringWheelMaxRotation = 180f;

    private Rigidbody rb;

    private void Awake()
    {
        carInputHandler = GetComponent<CarInputHandler>();

        //baixa o centro de massa do carro para nao capotar
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isColiding = true;
            player = other.gameObject;
            playerCameraY = player.GetComponentInChildren<Camera>().gameObject.transform;
            playerCameraX = player.GetComponentInChildren<Camera>().gameObject.transform;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isColiding = false;
        }
    }
    private IEnumerator FindPlayerWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for 'delay' seconds

        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player not found! Make sure the player has the 'Player' tag.");
        }

        playerCameraY = player.GetComponentInChildren<Camera>().gameObject.transform;
        playerCameraX = player.GetComponentInChildren<Camera>().gameObject.transform;


    }

    private void FixedUpdate()
    {
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
        UpdateSteeringWheel();
    }

    private void EnterCar()
    {
        player.GetComponent<CharacterController>().enabled = false;
        player.GetComponent<FirstPersonController>().playerCanMove = false;

        // Smoothly move and rotate the player
        player.transform.position = Vector3.MoveTowards(player.transform.position, driverSeat.position, 0.2f);
        player.transform.rotation = Quaternion.RotateTowards(player.transform.rotation, driverSeat.rotation, 0.2f);

        // Check if close enough to the target
        if (Vector3.Distance(player.transform.position, driverSeat.position) < 0.01f)
        {
            player.transform.position = driverSeat.position; // Snap to position
            player.transform.rotation = driverSeat.rotation; // Snap to rotation
            isTransiting = false;
            playerInsideCar = true;
        }
    }
    private void GetInput()
    {
        if (playerInsideCar == true)
        {
            horizontalInput = carInputHandler.SteerInput;
            verticalInput = carInputHandler.AccelerateInput;
            isBreaking = carInputHandler.BrakeInput;
        }
    }

    private void HandleMotor()
    {
        frontLeftWheelCollider.motorTorque = verticalInput * motorForce;
        frontRightWheelCollider.motorTorque = verticalInput * motorForce;
        currentbreakForce = isBreaking ? breakForce : 0f;
        ApplyBreaking();
    }

    private void ApplyBreaking()
    {
        frontRightWheelCollider.brakeTorque = currentbreakForce;
        frontLeftWheelCollider.brakeTorque = currentbreakForce;
        rearLeftWheelCollider.brakeTorque = currentbreakForce;
        rearRightWheelCollider.brakeTorque = currentbreakForce;
    }

    private void HandleSteering()
    {
        currentSteerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

    private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isColiding)
                isTransiting = true;

        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            VanFlip();
        }

        if (isTransiting)
        {
            if (playerInsideCar) ExitCar();
            else EnterCar();
        }

        if (playerInsideCar)
        {
            player.transform.position = driverSeat.position;
            carCameraLocker();
        }
    }

    private void carCameraLocker()
    {
        if (playerInsideCar)
        {
            Vector3 carEulerAngles = carCamera.rotation.eulerAngles;
            Vector3 cameraEulerAngles = playerCameraY.localEulerAngles;

            if (cameraEulerAngles.y > 180f) cameraEulerAngles.y -= 360f;
            if (carEulerAngles.y > 180f) carEulerAngles.y -= 360f;

            float cameraYRelativeToCar = Mathf.DeltaAngle(carEulerAngles.y, cameraEulerAngles.y);
            cameraYRelativeToCar = Mathf.Clamp(cameraYRelativeToCar, -90f, 90f);

            float lockedY = carEulerAngles.y + cameraYRelativeToCar;
            playerCameraY.rotation = Quaternion.Euler(carEulerAngles.x, lockedY, carEulerAngles.z);
        }
    }

    private void UpdateSteeringWheel()
    {
        if (playerInsideCar)
        {
            float steeringAngle = horizontalInput * steeringWheelMaxRotation;
            steeringWheel.localRotation = Quaternion.Euler(0f, 0f, -steeringAngle);

        }
    }



    private void ExitCar()
    {
        //set position
        player.transform.position = exitPoint.position;

        //set player camera direction in exit
        playerCameraX.localRotation = Quaternion.Euler(0f, 0f, 0f);
        playerCameraY.localRotation = Quaternion.Euler(0f, 0f, 0f);

        //enables the player controller
        player.GetComponent<CharacterController>().enabled = true;
        player.GetComponent<FirstPersonController>().playerCanMove = true;

        if (player.transform.position == exitPoint.position)
        {
            isTransiting = false;
            playerInsideCar = false;
        }
    }

    private void VanFlip()
    {
        Vector3 currentEuler = transform.eulerAngles;
        transform.eulerAngles = new Vector3(0f, currentEuler.y, 0f);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}