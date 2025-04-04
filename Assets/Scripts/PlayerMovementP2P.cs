using UnityEngine;
using Unity.Netcode;

public class PlayerMovementP2P : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float groundDrag = 5f;
    public float jumpForce = 12f;
    public float jumpCooldown = 0.25f;
    public float airMultiplier = 0.4f;
    public float gravityMultiplier = 2f;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;

    [Header("Network")]
    public float networkUpdateRate = 0.1f;
    private float lastNetworkUpdateTime;

    private NetworkVariable<PlayerNetworkState> networkState = new NetworkVariable<PlayerNetworkState>(
        new PlayerNetworkState(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private bool grounded;
    private bool readyToJump = true;
    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private Rigidbody rb;
    private Transform orientation;

    private struct PlayerNetworkState : INetworkSerializable
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public bool IsGrounded;
        public bool IsJumping;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref IsGrounded);
            serializer.SerializeValue(ref IsJumping);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        orientation = transform.Find("Orientation");
        if (orientation == null) orientation = transform;
    }

    private void Start()
    {
        if (!IsOwner)
        {
            rb.isKinematic = true;
            return;
        }

        rb.freezeRotation = true;
        rb.linearDamping = 0f;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Ground check
        grounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            playerHeight * 0.5f + groundCheckDistance,
            groundLayer
        );

        MyInput();
        SpeedControl();

        // Handle drag
        rb.linearDamping = grounded ? groundDrag : 0f;

        // Network updates
        if (Time.time - lastNetworkUpdateTime > networkUpdateRate)
        {
            lastNetworkUpdateTime = Time.time;
            UpdateNetworkState();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            // Apply network state to non-owned players
            if (!rb.isKinematic)
            {
                rb.position = Vector3.Lerp(rb.position, networkState.Value.Position, 0.3f);
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, networkState.Value.Velocity, 0.3f);
            }
            return;
        }

        MovePlayer();
    }

    private void UpdateNetworkState()
    {
        var state = new PlayerNetworkState
        {
            Position = rb.position,
            Velocity = rb.linearVelocity,
            IsGrounded = grounded,
            IsJumping = !readyToJump
        };

        if (IsServer)
        {
            networkState.Value = state;
        }
        else
        {
            UpdateServerStateServerRpc(state);
        }
    }

    [ServerRpc]
    private void UpdateServerStateServerRpc(PlayerNetworkState state)
    {
        networkState.Value = state;
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        // Calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // Apply forces based on ground state
        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // Reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Disable unnecessary components on remote players
            var camera = GetComponentInChildren<Camera>();
            if (camera != null) camera.enabled = false;
            var audioListener = GetComponentInChildren<AudioListener>();
            if (audioListener != null) audioListener.enabled = false;
        }
    }
}