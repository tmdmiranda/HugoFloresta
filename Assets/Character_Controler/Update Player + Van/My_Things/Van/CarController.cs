using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    private float horizontalInput, verticalInput;
    private float currentSteerAngle, currentbreakForce;
    private bool isBreaking;
    public bool playerInsideCar = false;

    public Transform player;

    private CarInputHandler carInputHandler;

    [Header("Drive Settings")]
    private bool isTransiting;
    [SerializeField] public Transform driverSeat;
    [SerializeField] public Transform exitPoint;
    private float transitionSpeed = 0.2f;
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

    private void FixedUpdate()
    {
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
        UpdateSteeringWheel();
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
        if (playerInsideCar && isTransiting) ExitCar();
        else if (!playerInsideCar && isTransiting) EnterCar();

        if (Input.GetKeyDown(KeyCode.E))
        {
            isTransiting = true;
        }

        if (playerInsideCar == true)
        {
            player.position = driverSeat.position;
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
        if (steeringWheel != null)
        {
            float steeringAngle = horizontalInput * steeringWheelMaxRotation;
            steeringWheel.localRotation = Quaternion.Euler(0f, 0f, -steeringAngle);
        }
    }

    private void EnterCar()
    {
        player.GetComponent<CharacterController>().enabled = false;
        player.GetComponent<FirstPersonController>().playerCanMove = false;

        player.position = driverSeat.position;
        player.rotation = Quaternion.Slerp(player.rotation, driverSeat.rotation, transitionSpeed);

        if (player.position == driverSeat.position)
        {
            isTransiting = false;
            playerInsideCar = true;
        }
    }

    private void ExitCar()
    {
        player.position = exitPoint.position;

        player.GetComponent<CharacterController>().enabled = true;
        player.GetComponent<FirstPersonController>().playerCanMove = true;

        if (player.position == exitPoint.position)
        {
            isTransiting = false;
            playerInsideCar = false;
        }
    }
}