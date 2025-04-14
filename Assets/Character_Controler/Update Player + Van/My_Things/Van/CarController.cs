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

    /// Mouse Input sttings
    private Vector3 lastMousePosition;
    private float mouseIdleTime = 0f;

    [Header("Drive Settings")]
    private bool isTransiting;
    [SerializeField] public Transform driverSeat;
    [SerializeField] public Transform exitPoint;
    private float transitionSpeed = 0.2f;
    [SerializeField] public Transform carCamera;
    [SerializeField] public Transform playerCameraY;
    [SerializeField] public Transform playerCameraX;

    [Header ("Car Settings")]
    [SerializeField] private float motorForce;
    [SerializeField] private float breakForce;
    [SerializeField] private float maxSteerAngle;

    [Header ("Wheel Colliders")]
    [SerializeField] private WheelCollider frontLeftWheelCollider;
    [SerializeField] private WheelCollider frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider;
    [SerializeField] private WheelCollider rearRightWheelCollider;

    [Header("Wheels")]
    [SerializeField] private Transform frontLeftWheelTransform;
    [SerializeField] private Transform frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform;
    [SerializeField] private Transform rearRightWheelTransform;

    private void FixedUpdate()
    {
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
    }

    private void GetInput()
    {
        if (playerInsideCar == true)
        {
            // Steering Input
            horizontalInput = Input.GetAxis("Horizontal");

            // Acceleration Input
            verticalInput = Input.GetAxis("Vertical");

            // Breaking Input
            isBreaking = Input.GetKey(KeyCode.Space);
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
            CameraUpdateCar();
        }
    }

    private void CameraUpdateCar()
    {
        if (playerInsideCar)
        {
            // Update the player camera position and rotation to match the car camera
            Quaternion newRotation = playerCameraX.rotation;
            newRotation.eulerAngles = new Vector3(carCamera.rotation.eulerAngles.x, newRotation.eulerAngles.y, newRotation.eulerAngles.z);
            playerCameraX.rotation = newRotation;

        }
    }

    private void EnterCar()
    {
        // Disable player movement and physics
        player.GetComponent<CharacterController>().enabled = false;
        player.GetComponent<FirstPersonController>().playerCanMove = false;

        //Set the player position to the driver seat
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
        //Set the player position to the exit point
        player.position = exitPoint.position;

        // Enable player movement and physics
        player.GetComponent<CharacterController>().enabled = true;
        player.GetComponent<FirstPersonController>().playerCanMove = true;

        if (player.position == exitPoint.position)
        {
            isTransiting = false;
            playerInsideCar = false;
        }

    }
}