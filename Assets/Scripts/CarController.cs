using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class CarController : NetworkBehaviour
{
    private float horizontalInput, verticalInput;
    private float currentSteerAngle, currentbreakForce;
    private bool isBreaking;
    private bool isColiding;
    public bool playerInsideCar = false;
    public GameObject player;


    [Header("Seat Settings")]
    [SerializeField] private Transform[] seats = new Transform[6];
    private NetworkList<ulong> seatOccupants;
    private bool HasDriver;

    [Header("Camera Settings")]
    [SerializeField] private float rotationSmoothness = 5f;
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform carCamera;
    [SerializeField] private Transform playerCameraY;
    [SerializeField] private Transform playerCameraX;
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

    [Header("Camera Settings")]
    [SerializeField] private float cameraFollowSpeed = 5f;
    [SerializeField] private float deadzoneAngle = 5f;
    [SerializeField] private float maxLookAngle = 90f;

    private void HandleVehicleCamera()
    {
        if (!playerInsideCar) return;

        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        float targetXRotation = playerCameraX.localEulerAngles.x - mouseY;
        targetXRotation = Mathf.Clamp(NormalizeAngle(targetXRotation), -maxLookAngle, maxLookAngle);

        if (Mathf.Abs(targetXRotation) < deadzoneAngle)
        {
            targetXRotation = 0f;
        }

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
        rb.isKinematic = true; // Start as kinematic until ownership is set
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);

        seatOccupants = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Server retains ownership permanently
        if (IsServer)
        {
            rb.isKinematic = false;

            // Initialize seat occupants on server
            seatOccupants.Clear();
            for (int i = 0; i < seats.Length; i++)
            {
                seatOccupants.Add(0); // 0 means empty seat
            }
        }
        else
        {
            rb.isKinematic = true; // Clients don't control physics

            // Clients wait for server to populate seatOccupants
            // No need to initialize here as it will sync automatically
        }
    }



    public void EnableVanPhysics()
    {
        rb.isKinematic = false;
    }
    private void FixedUpdate()
    {
        if (!IsSpawned) return;

        GetInput();

        // Only the owner handles physics
        if (IsOwner)
        {
            HandleMotor();
            HandleSteering();
        }

        UpdateWheels();
        UpdateSteeringWheel();
    }


    private void OnTriggerEnter(Collider other)
    {
        var netObj = other.GetComponent<NetworkObject>();
        Debug.Log($"OnTriggerEnter: {other.name}, IsLocalPlayer: {netObj?.IsLocalPlayer}");

        if (netObj.IsLocalPlayer == false) return;


        if (other.CompareTag("Player"))
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

        var netObj = other.GetComponent<NetworkObject>();
        Debug.Log($"OnTriggerEnter: {other.name}, IsLocalPlayer: {netObj?.IsLocalPlayer}");

        if (netObj == null || !netObj.IsLocalPlayer) return;
        if (other.CompareTag("Player"))
        {

            isColiding = false;

        }
    }

    private void Update()
    {
        var netObj = player?.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"E pressed, isColiding: {isColiding}, playerInsideCar: {playerInsideCar}");
            if (isColiding == true && !playerInsideCar)
                EnterCar();
            else if (playerInsideCar)
                ExitCar();
        }

        if (playerInsideCar)
        {
            Debug.Log($"Player inside car, player: {player.name}, cameraY: {playerCameraY?.name}, cameraX: {playerCameraX?.name}");
            HandleCamera();
            SearchOccupiedSeatsIfLocalPlayerIsSeated();
        }
    }

    private void HandleCamera()
    {
        if (!playerInsideCar || playerCameraY == null || playerCameraX == null || carCamera == null)
            return;

        if (playerCameraY.parent != carCamera)
        {

            playerCameraY.localPosition = Vector3.zero;
            playerCameraY.localRotation = Quaternion.identity;
        }
    }

    private void EnterCar()
    {
        if (!isColiding || player == null) return;

        var playerNetObj = player.GetComponent<NetworkObject>();
        if (playerNetObj == null) return;

        // Wait until seatOccupants is properly initialized
        if (seatOccupants.Count != seats.Length)
        {
            Debug.LogWarning("Seat occupants not initialized yet");
            return;
        }

        // Find first available seat locally
        int seatIndex = -1;
        for (int i = 0; i < seats.Length; i++)
        {
            if (seatOccupants[i] == 0)
            {
                seatIndex = i;
                break;
            }
        }

        if (seatIndex == -1)
        {
            Debug.Log("No available seats");
            return;
        }

        Debug.Log($"Attempting to enter car at seat {seatIndex}");
        RequestEnterCarServerRpc(playerNetObj.OwnerClientId, seatIndex);
    }


    [ServerRpc(RequireOwnership = false)]
    private void RequestEnterCarServerRpc(ulong clientId, int requestedSeat)
    {
        // Validate seat index
        if (requestedSeat < 0 || requestedSeat >= seatOccupants.Count) return;

        // Validate seat availability
        if (seatOccupants[requestedSeat] != 0) return;

        Debug.Log($"Assigning seat {requestedSeat} to client {clientId}");

        // Occupy seat (no ownership transfer needed)
        seatOccupants[requestedSeat] = clientId;

        // If this is the driver seat, set HasDriver
        if (requestedSeat == 0)
        {
            HasDriver = true;
        }

        MovePlayerToSeatClientRpc(clientId, requestedSeat);
    }

    private void SearchOccupiedSeatsIfLocalPlayerIsSeated()
    {
        if (player == null || !player.GetComponent<NetworkObject>().IsLocalPlayer) return;

        // Find which seat the local player is occupying
        for (int i = 0; i < seatOccupants.Count; i++)
        {
            if (seatOccupants[i] == player.GetComponent<NetworkObject>().OwnerClientId)
            {
                player.transform.SetPositionAndRotation(
                    seats[i].position,
                    seats[i].rotation
                );
                break;
            }
        }
    }

    [ClientRpc]
    private void MovePlayerToSeatClientRpc(ulong clientId, int seatIndex)
    {
        // Find player object
        NetworkObject playerNetObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerNetObj == null) return;

        // Position player
        playerNetObj.transform.SetPositionAndRotation(
            seats[seatIndex].position,
            seats[seatIndex].rotation
        );

        // Disable components for all clients
        if (playerNetObj.TryGetComponent<CharacterController>(out var controller))
            controller.enabled = false;

        if (playerNetObj.TryGetComponent<FirstPersonController>(out var fps))
            fps.enabled = false; // Disable completely instead of just playerCanMove

        // Configure local player specifics
        if (playerNetObj.IsLocalPlayer)
        {
            player = playerNetObj.gameObject;
            playerInsideCar = true;
            HasDriver = (seatIndex == 0);

            // Setup camera
            playerCameraY = player.transform;
            if (playerCameraY != null && playerCameraY.childCount > 0)
                playerCameraX = playerCameraY.GetChild(0);
        }
    }


    private void SendInputsToServer()
    {
        if (!IsOwner || !playerInsideCar) return;

        // Send inputs to server
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
        UpdateSteeringWheel();
    }
    private void GetInput()
    {
        if (playerInsideCar && HasDriver)
        {
            horizontalInput = carInputHandler.SteerInput;
            verticalInput = carInputHandler.AccelerateInput;
            isBreaking = carInputHandler.BrakeInput;

            // Non-owners send inputs to the server
            if (!IsOwner)
            {
                SendInputsToServerRpc(horizontalInput, verticalInput, isBreaking);
            }
        }
        else
        {
            horizontalInput = 0;
            verticalInput = 0;
            isBreaking = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendInputsToServerRpc(float steer, float accelerate, bool brake)
    {
        // Only the server (owner) applies these inputs
        horizontalInput = steer;
        verticalInput = accelerate;
        isBreaking = brake;
    }

    [ClientRpc]
    private void ReceiveInputsClientRpc(float steer, float accelerate, bool brake, ClientRpcParams rpcParams = default)
    {
        // Only the owner processes these inputs
        if (!IsOwner) return;

        horizontalInput = steer;
        verticalInput = accelerate;
        isBreaking = brake;
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


    [ServerRpc]
    private void ExitCarServerRpc(ulong playerId, int seatIndex, Vector3 exitPosition)
    {
        ExitCarClientRpc(playerId, exitPosition);
    }

    [ClientRpc]
    private void ExitCarClientRpc(ulong clientId, Vector3 exitPosition)
    {

        // Find which seat this player was in
        int seatIndex = -1;
        for (int i = 0; i < seatOccupants.Count; i++)
        {
            if (seatOccupants[i] == clientId)
            {
                seatIndex = i;
                break;
            }
        }

        if (seatIndex != -1)
        {
            // Only the server can actually modify the NetworkList
            if (IsServer)
            {
                seatOccupants[seatIndex] = 0;

                // If this was the driver, clear HasDriver
                if (seatIndex == 0)
                {
                    HasDriver = false;
                }
            }
        }
        NetworkObject playerNetObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerNetObj == null) return;

        // Re-enable components for all clients
        if (playerNetObj.TryGetComponent<CharacterController>(out var controller))
            controller.enabled = true;

        if (playerNetObj.TryGetComponent<FirstPersonController>(out var fps))
            fps.enabled = true;

        // Only handle local player specific logic
        if (playerNetObj.IsLocalPlayer)
        {
            // Reset position
            playerNetObj.transform.position = exitPosition;

            // Reset car state
            playerInsideCar = false;
            HasDriver = false;
            player = null;
            playerCameraX = null;
            playerCameraY = null;

            // Reset input
            horizontalInput = 0;
            verticalInput = 0;
            isBreaking = false;
        }
    }

    [ServerRpc]
    private void ExitCarServerRpc(ulong exitingPlayerId, Vector3 exitPosition)
    {
        // Find which seat the player is in
        int seatIndex = -1;
        for (int i = 0; i < seatOccupants.Count; i++)
        {
            if (seatOccupants[i] == exitingPlayerId)
            {
                seatIndex = i;
                break;
            }
        }

        if (seatIndex == -1) return;

        // Free the seat
        seatOccupants[seatIndex] = 0;
        ExitCarClientRpc(exitingPlayerId, exitPosition);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestExitCarServerRpc(ulong clientId, int seatIndex, Vector3 exitPos)
    {
        if (seatOccupants[seatIndex] != clientId) return;

        // Free seat
        seatOccupants[seatIndex] = 0;

        ExitCarClientRpc(clientId, exitPos);
    }



    private void ExitCar()
    {
        if (player == null) return;

        var playerNetObj = player.GetComponent<NetworkObject>();
        if (!playerNetObj.IsLocalPlayer) return;

        // Find occupied seat
        int seatIndex = -1;
        for (int i = 0; i < seatOccupants.Count; i++)
        {
            if (seatOccupants[i] == playerNetObj.OwnerClientId)
            {
                seatIndex = i;
                break;
            }
        }
        if (seatIndex == -1) return;

        RequestExitCarServerRpc(playerNetObj.OwnerClientId, seatIndex, exitPoint.position);
    }
}