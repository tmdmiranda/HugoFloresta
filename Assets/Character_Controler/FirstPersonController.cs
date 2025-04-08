using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

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

    [Header("Network")]
    [SerializeField] private float networkUpdateRate = 0.008f; // ~120Hz
    private float lastNetworkUpdateTime;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private Transform playerBody;

    // Movement
    private Vector3 currentMovement;
    private float verticalRotation;
    private float CurrentSpeed => walkSpeed * (playerInputHandler.SprintTriggered ? sprintMultiplier : 1f);

    // Network state
    private NetworkVariable<NetworkState> networkState = new NetworkVariable<NetworkState>(
        new NetworkState(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Remote player smoothing
    private Queue<Vector3> positionBuffer = new Queue<Vector3>(4);
    private Queue<float> rotationBuffer = new Queue<float>(4);
    private Vector3 targetPosition;
    private float targetRotation;

    private struct NetworkState : INetworkSerializable
    {
        public Vector3 position;
        public float rotation;
        public double timestamp;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref timestamp);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            mainCamera.enabled = false;
            mainCamera.GetComponent<AudioListener>().enabled = false;
            return;
        }

        mainCamera.enabled = true;
        mainCamera.GetComponent<AudioListener>().enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    private void Update()
    {
        if (IsOwner)
        {
            HandleRotation();
            HandleMovement();
            HandleCrouching();

            if (Time.time - lastNetworkUpdateTime >= networkUpdateRate)
            {
                SendNetworkUpdate();
                lastNetworkUpdateTime = Time.time;
            }
        }
        else
        {
            SmoothRemoteMovement();
        }
    }

    private void SendNetworkUpdate()
    {
        UpdateServerStateServerRpc(new NetworkState
        {
            position = transform.position,
            rotation = verticalRotation,
            timestamp = Time.timeAsDouble
        });
    }

    [ServerRpc]
    private void UpdateServerStateServerRpc(NetworkState state)
    {
        networkState.Value = state;
    }

    private void SmoothRemoteMovement()
    {
        // Buffer incoming states
        positionBuffer.Enqueue(networkState.Value.position);
        rotationBuffer.Enqueue(networkState.Value.rotation);
        if (positionBuffer.Count > 4)
        {
            positionBuffer.Dequeue();
            rotationBuffer.Dequeue();
        }

        // Calculate moving averages
        targetPosition = Vector3.zero;
        foreach (Vector3 pos in positionBuffer) targetPosition += pos;
        targetPosition /= positionBuffer.Count;

        targetRotation = 0f;
        foreach (float rot in rotationBuffer) targetRotation += rot;
        targetRotation /= rotationBuffer.Count;

        // Direct interpolation
        transform.position = Vector3.Lerp(transform.position, targetPosition, 25f * Time.deltaTime);
        verticalRotation = Mathf.Lerp(verticalRotation, targetRotation, 25f * Time.deltaTime);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();
        currentMovement.x = worldDirection.x * CurrentSpeed;
        currentMovement.z = worldDirection.z * CurrentSpeed;

        HandleJumping();
        characterController.Move(currentMovement * Time.deltaTime);
    }

    private Vector3 CalculateWorldDirection()
    {
        Vector3 inputDirection = new Vector3(playerInputHandler.MovementInput.x, 0f, playerInputHandler.MovementInput.y);
        return transform.TransformDirection(inputDirection).normalized;
    }

    private void HandleJumping()
    {
        if (characterController.isGrounded)
        {
            currentMovement.y = -0.5f;
            if (playerInputHandler.JumpTriggered) currentMovement.y = jumpForce;
        }
        else
        {
            currentMovement.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }
    }

    private void HandleRotation()
    {
        float mouseX = playerInputHandler.RotationInput.x * mouseSensitivity;
        float mouseY = playerInputHandler.RotationInput.y * mouseSensitivity;

        transform.Rotate(0, mouseX, 0);
        verticalRotation = Mathf.Clamp(verticalRotation - mouseY, -upDownLookRange, upDownLookRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    private void ApplyCrouchState(bool shouldCrouch)
    {
        if (shouldCrouch)
        {
            characterController.height = 1f;
            characterController.center = new Vector3(0, -0.5f, 0);
            mainCamera.transform.localPosition = new Vector3(0, 0, 0);
            playerBody.localPosition = new Vector3(0, -0.5f, 0);
            playerBody.localScale = new Vector3(1, 0.5f, 1);
        }
        else
        {
            characterController.height = 2f;
            characterController.center = new Vector3(0, 0, 0);
            mainCamera.transform.localPosition = new Vector3(0, 0.7f, 0);
            playerBody.localPosition = new Vector3(0, 0, 0);
            playerBody.localScale = new Vector3(1, 1, 1);
        }
    }

    private void HandleCrouching()
    {
        if (playerInputHandler != null)
        {
            ApplyCrouchState(playerInputHandler.CrouchTriggered);
        }
    }
}