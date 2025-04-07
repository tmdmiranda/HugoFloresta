using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class FirstPersonController : NetworkBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 6.0f;
    [SerializeField] private float sprintMultiplier = 2.0f;

    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce = 4.0f;
    [SerializeField] private float gravityMultiplier = 1.0f;

    [Header("Look Parameters")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownLookRange = 80f;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private Transform playerBody;

    private Vector3 currentMovement;
    private float verticalRotation;
    private float CurrentSpeed => walkSpeed * (playerInputHandler.SprintTriggered ? sprintMultiplier : 1);

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Disable camera & audio for remote players
            if (mainCamera != null)
            {
                mainCamera.enabled = false;
                mainCamera.GetComponent<AudioListener>().enabled = false;
            }
            return;
        }

        // Only enable for local player
        if (mainCamera != null)
        {
            mainCamera.enabled = true;
            mainCamera.GetComponent<AudioListener>().enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!IsOwner) return;
        
        HandleMovement();
        HandleRotation();
        HandleCrouching();
    }

    private Vector3 CalculateWorldDirection()
    {
        Vector3 inputDirection = new Vector3(playerInputHandler.MovementInput.x, 0f, playerInputHandler.MovementInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        return worldDirection.normalized;
    }

    private void HandleJumping()
    {
        if (characterController.isGrounded)
        {
            currentMovement.y = -0.5f;

            if (playerInputHandler.JumpTriggered)
            {
                currentMovement.y = jumpForce;
            }
        }
        else
        {
            currentMovement.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();
        currentMovement.x = worldDirection.x * CurrentSpeed;
        currentMovement.z = worldDirection.z * CurrentSpeed;

        HandleJumping();
        characterController.Move(currentMovement * Time.deltaTime);
    }

    private void ApplyHorizontalRotation(float rotationAmount)
    {
        transform.Rotate(0, rotationAmount, 0);
    }

    private void ApplyVerticalRotation(float rotationAmount)
    {
        verticalRotation = Mathf.Clamp(verticalRotation - rotationAmount, -upDownLookRange, upDownLookRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    private void HandleRotation()
    {
        float mouseXRotation = playerInputHandler.RotationInput.x * mouseSensitivity;
        float mouseYRotation = playerInputHandler.RotationInput.y * mouseSensitivity;

        ApplyHorizontalRotation(mouseXRotation);
        ApplyVerticalRotation(mouseYRotation);
    }

    private void HandleCrouching()
    {
        if (playerInputHandler.CrouchTriggered)
        {
            characterController.height = 1f; 
            characterController.center = new Vector3(0, -0.5f, 0);
            mainCamera.transform.localPosition = new Vector3(0, 0, 0);
            
            //Transform Body
            playerBody.localPosition = new Vector3(0, -0.5f, 0);
            playerBody.localScale = new Vector3(1, 0.5f, 1);
        }
        else
        {   
            characterController.height = 2f;
            characterController.center = new Vector3(0, 0, 0);
            mainCamera.transform.localPosition = new Vector3(0, 0.7f, 0);

            //Transform Body
            playerBody.localPosition = new Vector3(0, 0, 0);
            playerBody.localScale = new Vector3(1, 1, 1);
        }
    }
}