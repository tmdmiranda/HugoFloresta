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
    [SerializeField] private Transform playerBody;    [Header("Animation")]
    [SerializeField] private float animationSmoothTime = 0.1f;
    [SerializeField] private float walkAnimationSpeed = 0.5f;
    [SerializeField] private float sprintAnimationSpeed = 1f;
    [SerializeField] private float crouchAnimationSpeed = 0.3f;    [Header("Crouch Parameters")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 crouchCenter = new Vector3(0, -0.5f, 0);
    [SerializeField] private Vector3 standingCameraPosition = new Vector3(0, 0.7f, 0);
    [SerializeField] private Vector3 crouchCameraPosition = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 standingBodyPosition = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 crouchBodyPosition = new Vector3(0, -0.5f, 0);
    [SerializeField] private Vector3 crouchBodyScale = new Vector3(1, 0.5f, 1);
    
    // Original transforms (captured at start)
    private Vector3 originalBodyScale;
    private Vector3 originalBodyPosition;
    private Vector3 originalCameraPosition;
    private Vector3 originalCharacterCenter;
    private float originalCharacterHeight;

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
    private NetworkVariable<bool> isCrouchingNetwork = new NetworkVariable<bool>();    [Header("Network Synchronization")]
    [SerializeField] private float networkSendRate = 20f; // How often to send updates per second
    private float lastNetworkSendTime;
    
    // Network position/rotation for non-owners
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
        Vector3.zero, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner
    );
    
    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner
    );    void Start()
    {
        // Clean up conflicting components for ALL players (owners and non-owners)
        // This must be done before any other setup to prevent position conflicts
        CleanupConflictingComponents();

        // Capture original transform values
        if (playerBody != null)
        {
            originalBodyScale = playerBody.localScale;
            originalBodyPosition = playerBody.localPosition;
        }
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.localPosition;
        }
        if (characterController != null)
        {
            originalCharacterCenter = characterController.center;
            originalCharacterHeight = characterController.height;
        }

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

        // Subscribe to network position changes for interpolation
        if (!IsOwner)
        {
            networkPosition.OnValueChanged += OnNetworkPositionChanged;
            networkRotation.OnValueChanged += OnNetworkRotationChanged;
        }
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
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Ensure conflicting components are disabled when network object spawns
        // This is especially important for clients joining the game
        CleanupConflictingComponents();
        
        Debug.Log($"FirstPersonController spawned for client {OwnerClientId}. IsOwner: {IsOwner}");
    }

    void Update()
    {
        if (!IsOwner) 
        {
            // Non-owners: interpolate to network position
            InterpolateToNetworkTransform();
            return;
        }

        if (playerCanLookAround)
        {
            HandleRotation();
        }
        if (playerCanMove)
        {
            HandleMovement();
            HandleCrouching();
            UpdateAnimations();
        }
        
        // Send network updates
        SendNetworkUpdates();
    }
    
    private void SendNetworkUpdates()
    {
        if (!IsOwner) return;
        
        // Send position/rotation updates at specified rate
        if (Time.time - lastNetworkSendTime >= (1f / networkSendRate))
        {
            networkPosition.Value = transform.position;
            networkRotation.Value = transform.rotation;
            lastNetworkSendTime = Time.time;
        }
    }
    
    private void OnNetworkPositionChanged(Vector3 previousValue, Vector3 newValue)
    {
        // This will be handled in InterpolateToNetworkTransform()
    }
    
    private void OnNetworkRotationChanged(Quaternion previousValue, Quaternion newValue)
    {
        // This will be handled in InterpolateToNetworkTransform()
    }
      private void InterpolateToNetworkTransform()
    {
        // Smoothly interpolate non-owner players to network position
        float lerpRate = Time.deltaTime * 15f; // Adjust speed as needed
        
        transform.position = Vector3.Lerp(transform.position, networkPosition.Value, lerpRate);
        transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation.Value, lerpRate);
        
        // Ensure player body stays synchronized with main transform
        if (playerBody != null)
        {
            // Force the player body to maintain its relative position to the main transform
            // This is crucial for keeping the 3D model aligned with the network position
            playerBody.position = transform.position + transform.TransformDirection(originalBodyPosition);
            playerBody.rotation = transform.rotation;
            
            // Apply current crouch state to the body
            if (isCrouchingNetwork.Value)
            {
                playerBody.localPosition = crouchBodyPosition;
                playerBody.localScale = crouchBodyScale;
            }
            else
            {
                playerBody.localPosition = originalBodyPosition;
                playerBody.localScale = originalBodyScale;
            }
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
    }    [ServerRpc]
    private void UpdateAnimationStateServerRpc(NetworkedAnimationState state)
    {
        // Update the NetworkVariable on the server
        networkedAnimationState.Value = state;
        
        // Send to all clients
        UpdateAnimationStateClientRpc(state);
    }[ClientRpc]
    private void UpdateAnimationStateClientRpc(NetworkedAnimationState state)
    {
        // Don't modify the NetworkVariable here - it's already been set by the server
        // Just update the animator directly on all clients
        animator.SetFloat("Speed", state.Speed);
        // Add other animation parameters as needed
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
    }    private void ApplyCrouchState(bool shouldCrouch)
    {
        if (characterController == null) return;

        if (shouldCrouch)
        {
            // Apply crouch configuration
            characterController.height = crouchHeight;
            characterController.center = crouchCenter;
            mainCamera.transform.localPosition = crouchCameraPosition;

            if (playerBody != null)
            {
                playerBody.localPosition = crouchBodyPosition;
                playerBody.localScale = crouchBodyScale;
            }
        }
        else
        {
            // Apply standing configuration using original values
            characterController.height = originalCharacterHeight;
            characterController.center = originalCharacterCenter;
            mainCamera.transform.localPosition = originalCameraPosition;

            if (playerBody != null)
            {
                playerBody.localPosition = originalBodyPosition;
                playerBody.localScale = originalBodyScale;
            }
        }
    }

    [ServerRpc]
    private void SetCrouchStateServerRpc(bool state)
    {
        isCrouchingNetwork.Value = state;
    }    /// <summary>
    /// Disables conflicting movement and camera systems to prevent position desync
    /// This ensures FirstPersonController has full control over player positioning
    /// </summary>
    private void CleanupConflictingComponents()
    {
        Debug.Log($"[FirstPersonController] Cleaning up conflicting components for {gameObject.name} (IsOwner: {IsOwner})");

        // Disable old P2P movement system
        var playerMovementP2P = GetComponent<PlayerMovementP2P>();
        if (playerMovementP2P != null)
        {
            playerMovementP2P.enabled = false;
            Debug.Log($"[FirstPersonController] Disabled conflicting PlayerMovementP2P component on {gameObject.name}");
        }

        // Disable conflicting camera scripts
        var moveCamera = GetComponent<MoveCamera>();
        if (moveCamera != null)
        {
            moveCamera.enabled = false;
            Debug.Log($"[FirstPersonController] Disabled conflicting MoveCamera component on {gameObject.name}");
        }

        var playerCam = GetComponent<PlayerCam>();
        if (playerCam != null)
        {
            playerCam.enabled = false;
            Debug.Log($"[FirstPersonController] Disabled conflicting PlayerCam component on {gameObject.name}");
        }

        // Also check for these components on child objects
        var childMoveCameras = GetComponentsInChildren<MoveCamera>(true); // Include inactive
        foreach (var cam in childMoveCameras)
        {
            if (cam != moveCamera && cam.enabled) // Don't disable the same one twice
            {
                cam.enabled = false;
                Debug.Log($"[FirstPersonController] Disabled conflicting MoveCamera component on child object: {cam.gameObject.name}");
            }
        }

        var childPlayerCams = GetComponentsInChildren<PlayerCam>(true); // Include inactive
        foreach (var cam in childPlayerCams)
        {
            if (cam != playerCam && cam.enabled) // Don't disable the same one twice
            {
                cam.enabled = false;
                Debug.Log($"[FirstPersonController] Disabled conflicting PlayerCam component on child object: {cam.gameObject.name}");
            }
        }

        // Disable any Rigidbody that might interfere (keeping it for physics but removing control)
        var rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.isKinematic = true; // Prevent physics interference with CharacterController
            Debug.Log($"[FirstPersonController] Set Rigidbody to kinematic to prevent physics conflicts on {gameObject.name}");
        }

        // For non-owners, also disable the CharacterController to prevent conflicts
        // The position will be controlled by network interpolation instead
        if (!IsOwner && characterController != null)
        {
            characterController.enabled = false;
            Debug.Log($"[FirstPersonController] Disabled CharacterController for non-owner player {gameObject.name}");
        }

        Debug.Log($"[FirstPersonController] Cleanup complete for {gameObject.name}");
    }
}
