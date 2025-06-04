using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

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

    [Header("Animation")]
    [SerializeField] private float animationSmoothTime = 0.1f;
    [SerializeField] private float walkAnimationSpeed = 0.5f;
    [SerializeField] private float sprintAnimationSpeed = 1f;
    [SerializeField] private float crouchAnimationSpeed = 0.3f;

    private float currentAnimationSpeed;
    private Vector2 currentAnimationBlend;
    private Vector2 animationVelocity;

    private Vector3 currentMovement;
    private float verticalRotation;
    private float CurrentSpeed => walkSpeed * (playerInputHandler.SprintTriggered ? sprintMultiplier : 1);

    [SerializeField] private Animator animator;

    // Networked animation parameters
    private struct NetworkedAnimationState : INetworkSerializable
    {
        public float Speed;
        public float Horizontal;
        public float Vertical;
        public bool IsGrounded;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Speed);
            serializer.SerializeValue(ref Horizontal);
            serializer.SerializeValue(ref Vertical);
            serializer.SerializeValue(ref IsGrounded);
        }
    }

    private NetworkVariable<NetworkedAnimationState> networkedAnimationState = new NetworkVariable<NetworkedAnimationState>();

    // Network crouch state
    private NetworkVariable<bool> isCrouchingNetwork = new NetworkVariable<bool>();

    void Start()
    {
        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            mainCamera.enabled = false;
            mainCamera.GetComponent<AudioListener>().enabled = false;
        }

        // Subscribe to crouch state changes
        isCrouchingNetwork.OnValueChanged += OnCrouchStateChanged;

        // Subscribe to animation state changes
        networkedAnimationState.OnValueChanged += OnAnimationStateChanged;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        isCrouchingNetwork.OnValueChanged -= OnCrouchStateChanged;
    }

    private void OnCrouchStateChanged(bool previousValue, bool newValue)
    {
        ApplyCrouchState(newValue);
    }

    private void OnAnimationStateChanged(NetworkedAnimationState previousValue, NetworkedAnimationState newValue)
    {
        animator.SetFloat("Speed", newValue.Speed);
        animator.SetFloat("Horizontal", newValue.Horizontal);
        animator.SetFloat("Vertical", newValue.Vertical);
        animator.SetBool("IsGrounded", newValue.IsGrounded);
    }

    void Update()
    {
        if (!IsOwner) return;

        if (playerCanLookAround)
        {
            HandleRotation();
        }
        if (playerCanMove)
        {
            HandleMovement();
            HandleCrouching();
            UpdateAnimations(); // Moved animation updates to a separate method
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

        private void UpdateAnimations()
    {
        // Calculate input magnitude (0 when not moving, 1 when moving at full speed)
        float inputMagnitude = playerInputHandler.MovementInput.magnitude;
        
        // Blend animation parameters smoothly
            currentAnimationBlend = Vector2.SmoothDamp(
            currentAnimationBlend, 
            playerInputHandler.MovementInput, 
            ref animationVelocity, 
            animationSmoothTime
        );

        // Set animation parameters
        if (isCrouchingNetwork.Value)
        {
            currentAnimationSpeed = Mathf.Lerp(0, crouchAnimationSpeed, inputMagnitude);
        }
        else if (playerInputHandler.SprintTriggered)
        {
            currentAnimationSpeed = Mathf.Lerp(0, sprintAnimationSpeed, inputMagnitude);
        }
        else
        {
            currentAnimationSpeed = Mathf.Lerp(0, walkAnimationSpeed, inputMagnitude);
        }

        animator.SetFloat("Speed", currentAnimationSpeed);

        NetworkedAnimationState state = new NetworkedAnimationState
        {
            Speed = currentAnimationSpeed,
            Horizontal = currentAnimationBlend.x,
            Vertical = currentAnimationBlend.y,
            IsGrounded = characterController.isGrounded
        };

        if (state.Speed != networkedAnimationState.Value.Speed ||
            state.Horizontal != networkedAnimationState.Value.Horizontal ||
            state.Vertical != networkedAnimationState.Value.Vertical ||
            state.IsGrounded != networkedAnimationState.Value.IsGrounded)
        {
            UpdateAnimationStateServerRpc(state);
        }
    }

    [ServerRpc]
    private void UpdateAnimationStateServerRpc(NetworkedAnimationState state)
    {
        UpdateAnimationStateClientRpc(state);
    }

    [ClientRpc]
    private void UpdateAnimationStateClientRpc(NetworkedAnimationState state)
    {
        networkedAnimationState.Value = state;
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
        if (playerInputHandler.CrouchTriggered != isCrouchingNetwork.Value)
        {
            SetCrouchStateServerRpc(playerInputHandler.CrouchTriggered);
        }
    }

    private void ApplyCrouchState(bool shouldCrouch)
    {
        if (characterController == null) return;

        if (shouldCrouch)
        {
            characterController.height = 1f;
            characterController.center = new Vector3(0, -0.5f, 0);
            mainCamera.transform.localPosition = new Vector3(0, 0, 0);

            if (playerBody != null)
            {
                playerBody.localPosition = new Vector3(0, -0.5f, 0);
                playerBody.localScale = new Vector3(1, 0.5f, 1);
            }
        }
        else
        {
            characterController.height = 2f;
            characterController.center = new Vector3(0, 0, 0);
            mainCamera.transform.localPosition = new Vector3(0, 0.7f, 0);

            if (playerBody != null)
            {
                playerBody.localPosition = new Vector3(0, 0, 0);
                playerBody.localScale = new Vector3(1, 1, 1);
            }
        }
    }

    [ServerRpc]
    private void SetCrouchStateServerRpc(bool state)
    {
        isCrouchingNetwork.Value = state;
    }
}
