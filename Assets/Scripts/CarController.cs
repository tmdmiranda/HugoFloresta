using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class CarController : NetworkBehaviour
{
    private float horizontalInput, verticalInput;
    private float currentSteerAngle, currentbreakForce;
    private bool isBreaking;

    private bool hasRequestedReparent = false;

    private bool isColiding;
    public bool playerInsideCar = false;
    public GameObject player;

    [Header("Seating System")]
    [SerializeField] private Transform[] seats; // Assign in inspector: [0]=driver, [1]=secondDriver, [2-5]=backseats
    private NetworkVariable<int> availableSeats = new NetworkVariable<int>(6); // Total seats count
    private NetworkList<ulong> seatedPlayers = new NetworkList<ulong>();
    private Dictionary<ulong, int> playerSeatIndices = new Dictionary<ulong, int>();

    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform carCamera; // Assign van's camera root
    [SerializeField] private Transform playerCameraY; // Camera yaw (horizontal)
    [SerializeField] private Transform playerCameraX; // Camera pitch (vertical)
    [Header("Camera Follow")]
    [SerializeField] private float turnFollowDeadzone = 0.1f;
    [SerializeField] private float maxTurnFollowSpeed = 2f;
    [SerializeField] private float turnFollowDropoffAngle = 90f;
    [SerializeField] private float cameraSnapThreshold = 5f;
    [SerializeField] private float cameraMaxRotation = 90f;
    [SerializeField] private float cameraSmoothingSpeed = 10f;
    [SerializeField] private float movementInfluenceZone = 15f;
    [SerializeField] private float movementInfluenceStrength = 0.5f;

    private CarInputHandler carInputHandler;
    private Rigidbody rb;

    [Header("Drive Settings")]
    private bool isTransiting;
    [SerializeField] private Transform driverSeat;
    [SerializeField] private Transform exitPoint;


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

    // Network synchronization variables
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();
    private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>();
    private NetworkVariable<Vector3> networkAngularVelocity = new NetworkVariable<Vector3>();

    [Header("Camera Settings")]
    [SerializeField] private float cameraFollowSpeed = 5f;
    [SerializeField] private float deadzoneAngle = 5f;
    [SerializeField] private float maxLookAngle = 90f;


    private void InitializeSeats()
    {
        // Ensure seats array is properly assigned
        if (seats == null || seats.Length != 6)
        {
            Debug.LogError("Seats not properly assigned! Need 6 seats (driver + secondDriver + 4 backseats)");
            return;
        }
    }

    private int GetNextAvailableSeat()
    {
        for (int i = 0; i < seats.Length; i++)
        {
            if (!playerSeatIndices.ContainsValue(i))
            {
                return i;
            }
        }
        return -1; // No available seats
    }

    [ServerRpc(RequireOwnership = false)]
    private void AssignSeatServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
    {
        if (seatedPlayers.Contains(clientId)) return;

        int seatIndex = GetNextAvailableSeat();
        if (seatIndex == -1) return;

        seatedPlayers.Add(clientId);
        playerSeatIndices[clientId] = seatIndex;
        availableSeats.Value--;

        AssignSeatClientRpc(clientId, seatIndex);
    }

    [ClientRpc]
    private void AssignSeatClientRpc(ulong clientId, int seatIndex)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // This is me - move to the assigned seat
            player.transform.position = seats[seatIndex].position;
            player.transform.rotation = seats[seatIndex].rotation;

            if (seatIndex == 0) // Driver seat
            {
                playerInsideCar = true;
                GetComponent<CarInputHandler>().enabled = true;
            }
        }
    }
    private void HandleVehicleCamera()
    {
        if (!playerInsideCar) return;

        // Get mouse input
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Calculate target vertical rotation
        float targetXRotation = playerCameraX.localEulerAngles.x - mouseY;
        targetXRotation = Mathf.Clamp(NormalizeAngle(targetXRotation), -maxLookAngle, maxLookAngle);

        // Apply deadzone near center
        if (Mathf.Abs(targetXRotation) < deadzoneAngle)
        {
            targetXRotation = 0f;
        }

        // Smoothly apply rotation
        playerCameraX.localRotation = Quaternion.Slerp(
            playerCameraX.localRotation,
            Quaternion.Euler(targetXRotation, 0f, 0f),
            cameraFollowSpeed * Time.deltaTime
        );
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
    private void Awake()
    {
        carInputHandler = GetComponent<CarInputHandler>();
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            seatedPlayers.OnListChanged += OnSeatedPlayersChanged;
        }

        InitializeSeats();
        if (IsClient && !IsHost)
        {
            rb.isKinematic = true; // Disable physics on clients
        }
        else
        {
            rb.isKinematic = true;
            Invoke(nameof(EnableVanPhysics), 0.1f);



        }
    }


    private void OnSeatedPlayersChanged(NetworkListEvent<ulong> changeEvent)
    {
        Debug.Log($"Seated players changed: {string.Join(",", seatedPlayers)}");
    }
    public void EnableVanPhysics()
    {
        rb.isKinematic = false;
    }

    private void FixedUpdate()
    {
        if (IsHost)
        {
            HandleHostPhysics();
        }
        else
        {
            HandleClientVisuals();
        }
    }

    private void HandleHostPhysics()
    {
        GetInput();
        HandleMotor();
        HandleSteering();

        // Update network variables with current state
        networkPosition.Value = rb.position;
        networkRotation.Value = rb.rotation;
        networkVelocity.Value = rb.linearVelocity;
        networkAngularVelocity.Value = rb.angularVelocity;

        UpdateWheels();
        UpdateSteeringWheel();
    }

    private void HandleClientVisuals()
    {
        if (!IsOwner) return; // Only run on local player's car
        // Smoothly interpolate to host's state
        rb.position = Vector3.Lerp(rb.position, networkPosition.Value, Time.fixedDeltaTime * 10f);
        rb.rotation = Quaternion.Lerp(rb.rotation, networkRotation.Value, Time.fixedDeltaTime * 10f);
        rb.linearVelocity = networkVelocity.Value;
        rb.angularVelocity = networkAngularVelocity.Value;

        UpdateWheels();
        UpdateSteeringWheel();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Only proceed if this collider belongs to local player
        if (other.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsOwner)
        {
            isColiding = true;
            player = other.gameObject;

            var cam = player.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                playerCameraY = player.transform;
                playerCameraX = cam.transform;
            }
            else
            {
                Debug.LogWarning("Camera not found on player entering car trigger.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsOwner)
        {
            isColiding = false;
        }
    }



    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isColiding && !playerInsideCar && !seatedPlayers.Contains(NetworkManager.Singleton.LocalClientId))
            {
                EnterCar();
            }
            else if (playerInsideCar || seatedPlayers.Contains(NetworkManager.Singleton.LocalClientId))
            {
                ExitSeatServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            VanFlip();
        }

        if (isTransiting && !playerInsideCar)
        {
            EnterCar();
        }

        if (playerInsideCar)
        {
            HandleCamera();
            player.transform.position = driverSeat.position;
        }
    }
    [SerializeField] private float rotationSmoothness = 5f;

    private void HandleCamera()
    {
        if (!playerInsideCar) return;

        // Only request the server to reparent once (you can add a bool flag to avoid repeated calls)
        if (!hasRequestedReparent)
        {
            RequestReparentServerRpc(NetworkManager.Singleton.LocalClientId);
            hasRequestedReparent = true;
        }

        // Then, locally for camera movement, you can parent camera pivots if they are not NetworkObjects

        // Example for local camera pivot (non-networked)
        if (playerCameraY.parent != carCamera)
        {
            playerCameraY.SetParent(carCamera, false);
            playerCameraY.localPosition = Vector3.zero;
            playerCameraY.localRotation = Quaternion.identity;
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void RequestReparentServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
    {
        // Find the NetworkObject of the client player (assuming you track it somewhere)
        var clientNetworkObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        if (clientNetworkObject != null)
        {
            // Reparent the player's NetworkObject transform under the car's transform (or desired parent)
            clientNetworkObject.transform.SetParent(transform, false);
        }
    }

    private void EnterCar()
    {
        int seatIndex = GetNextAvailableSeat();
        if (seatIndex == -1)
        {
            Debug.Log("Car is full!");
            return;
        }

        player.GetComponent<CharacterController>().enabled = false;
        player.GetComponent<FirstPersonController>().playerCanMove = false;

        AssignSeatServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ExitSeatServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
    {
        if (!seatedPlayers.Contains(clientId)) return;

        int seatIndex = playerSeatIndices[clientId];
        seatedPlayers.Remove(clientId);
        playerSeatIndices.Remove(clientId);
        availableSeats.Value++;

        ExitSeatClientRpc(clientId, seatIndex);
    }

    [ClientRpc]
    private void ExitSeatClientRpc(ulong clientId, int seatIndex)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Calculate safe exit position
            Vector3 exitPosition = exitPoint.position;
            if (Physics.Raycast(exitPoint.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f))
            {
                exitPosition.y = hit.point.y + 0.2f;
            }

            player.transform.position = exitPosition;

            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = true;

            var fpsController = player.GetComponent<FirstPersonController>();
            if (fpsController != null) fpsController.playerCanMove = true;

            if (seatIndex == 0) // Was driver
            {
                playerInsideCar = false;
                GetComponent<CarInputHandler>().enabled = false;
            }
        }
    }

    private void GetInput()
    {
        if (playerInsideCar)
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
        wheelTransform.SetPositionAndRotation(pos, rot);
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
        if (!IsOwner) return;

        // Calculate safe exit position
        Vector3 exitPosition = exitPoint.position;
        if (Physics.Raycast(exitPoint.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f))
        {
            exitPosition.y = hit.point.y + 0.2f;
        }

        ExitCarServerRpc(exitPosition);
    }

    [ServerRpc]
    private void ExitCarServerRpc(Vector3 exitPosition)
    {
        ExitCarClientRpc(exitPosition);
    }

    [ClientRpc]
    private void ExitCarClientRpc(Vector3 exitPosition)
    {
        player.transform.position = exitPosition;
        playerCameraX.localRotation = Quaternion.identity;
        playerCameraY.localRotation = Quaternion.identity;

        var controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = true;

        var fpsController = player.GetComponent<FirstPersonController>();
        if (fpsController != null)
        {
            fpsController.playerCanMove = true;
        }

        isTransiting = false;
        playerInsideCar = false;
    }

    private void VanFlip()
    {
        if (!IsOwner) return;
        VanFlipServerRpc();
    }

    [ServerRpc]
    private void VanFlipServerRpc()
    {
        transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Force sync
        networkPosition.Value = transform.position;
        networkRotation.Value = transform.rotation;
        networkVelocity.Value = Vector3.zero;
        networkAngularVelocity.Value = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsHost)
        {
            // Force immediate sync on collision
            networkPosition.Value = rb.position;
            networkRotation.Value = rb.rotation;
        }
    }
}