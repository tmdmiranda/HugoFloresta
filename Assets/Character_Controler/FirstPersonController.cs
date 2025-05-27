using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FirstPersonController : NetworkBehaviour
{
    [Header("Player Status")]
    [SerializeField] public bool playerCanMove = true;
    [SerializeField] public bool playerCanLookAround = true;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 6.0f;
    [SerializeField] private float sprintMultiplier = 2.0f;

    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce = 4.0f;
    [SerializeField] private float gravityMultiplier = 1.0f;

    [Header("Look Parameters")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownLookRange = 80f;

    [Header("Utility Parameters")]
    [SerializeField] private bool canUseHandBob = true;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private Transform playerBody;

    private Vector3 currentMovement;
    private float verticalRotation;
    private float CurrentSpeed => walkSpeed * (playerInputHandler.SprintTriggered ? sprintMultiplier : 1);

    [Header("Crouch Parameters")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private Vector3 crouchCenter = new Vector3(0, -0.5f, 0);
    [SerializeField] private Vector3 standCenter = Vector3.zero;
    [SerializeField] private Vector3 crouchCameraPosition = Vector3.zero;
    [SerializeField] private Vector3 standCameraPosition = new Vector3(0, 0.7f, 0);
    [SerializeField] private Vector3 crouchBodyScale = new Vector3(1, 0.5f, 1);

    // Networked crouch state
    private NetworkVariable<bool> isCrouching = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isCrouching.OnValueChanged += OnCrouchStateChanged;
    }

    private void OnCrouchStateChanged(bool wasCrouching, bool isNowCrouching)
    {
        if (!IsOwner)
        {
            ApplyCrouchState(isNowCrouching);
        }
    }
    void Update()
    {
        if (playerCanLookAround == true)
        {
            HandleRotation();
        }
        if (playerCanMove == true)
        {
            HandleMovement();
            HandleCrouching();
        }
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
        if (!IsOwner) return;

        if (playerInputHandler.CrouchTriggered)
        {
            // Toggle crouch state
            bool newCrouchState = !isCrouching.Value;
            isCrouching.Value = newCrouchState;

            // Apply locally immediately (network sync will handle other clients)
            ApplyCrouchState(newCrouchState);
        }
    }

    private void ApplyCrouchState(bool shouldCrouch)
    {
        if (shouldCrouch)
        {
            // Crouch setup
            characterController.height = crouchHeight;
            characterController.center = crouchCenter;
            mainCamera.transform.localPosition = crouchCameraPosition;
            playerBody.localScale = crouchBodyScale;
            playerBody.localPosition = crouchCenter;
        }
        else
        {
            // Stand setup
            characterController.height = standHeight;
            characterController.center = standCenter;
            mainCamera.transform.localPosition = standCameraPosition;
            playerBody.localScale = Vector3.one;
            playerBody.localPosition = Vector3.zero;
        }
    }
}

