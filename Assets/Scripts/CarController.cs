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

    private bool remoteDriver;


    [Header("Seat Settings")]
    [SerializeField] private Transform[] seats = new Transform[6];
    public NetworkList<ulong> seatOccupants = new NetworkList<ulong>();
    private bool HasDriver;

    [Header("Camera Settings")]
    [SerializeField] private Transform carCamera;
    [SerializeField] private Transform playerCameraY;
    [SerializeField] private Transform playerCameraX;
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


    private bool AmITheDriver()
    {
        if (player == null) return false;

        var netObj = player.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsLocalPlayer) return false;

        // Check if the local player is occupying the driver seat
        return seatOccupants.Count > 0 && seatOccupants[0] == netObj.OwnerClientId;

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
            seatOccupants.Clear();
            for (int i = 0; i < seats.Length; i++)
            {
                seatOccupants.Add(99);  // Consistent empty value
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

        Debug.Log("Am i driving" + AmITheDriver());
        if (AmITheDriver() == true)
        {
            GetInput();
        }

        // Only the owner handles physics
        if (IsOwner)
        {
            Debug.Log($"[CarController] FixedUpdate - IsOwner: {IsOwner}, playerInsideCar: {playerInsideCar}, HasDriver: {HasDriver}");

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

        Debug.Log("Seats" + seatOccupants[0] + " " + seatOccupants[1] + " " + seatOccupants[2] + " " + seatOccupants[3] + " " + seatOccupants[4] + " " + seatOccupants[5]);

        if (playerInsideCar)
        {
            SearchOccupiedSeatsIfLocalPlayerIsSeated();
        }
    }


    private void EnterCar()
    {
        if (!isColiding || player == null) return;

        var playerNetObj = player.GetComponent<NetworkObject>();
        if (playerNetObj == null) return;

        // Try to claim driver seat (0) first
        int preferredSeat = 0;

        // If driver seat taken, find first available passenger seat
        if (seatOccupants.Count > 0 && seatOccupants[0] != 99)
        {
            for (int i = 1; i < seats.Length; i++)
            {
                if (seatOccupants[i] == 99)
                {
                    preferredSeat = i;
                    break;
                }
            }
        }

        Debug.Log("Preferred seat: " + preferredSeat);
        RequestEnterCarServerRpc(playerNetObj.OwnerClientId, preferredSeat);
    }


    [ServerRpc(RequireOwnership = false)]
    private void RequestEnterCarServerRpc(ulong clientId, int requestedSeat)
    {
        // Validate seat
        if (requestedSeat < 0 || requestedSeat >= seats.Length) return;
        if (seatOccupants[requestedSeat] != 99) return;

        Debug.Log($"[CarController] RequestEnterCarServerRpc - ClientId: {clientId}, RequestedSeat: {requestedSeat}");
        // DRIVER SEAT (0) - Only allow if empty
        if (requestedSeat == 0)
        {
            seatOccupants[0] = clientId;
            HasDriver = true;
        }

        // PASSENGER SEATS (1+)
        else
        {
            // Find first available passenger seat if requested is taken
            for (int i = 1; i < seats.Length; i++)
            {
                if (seatOccupants[i] == 99)
                {
                    seatOccupants[i] = clientId;
                    break;
                }
            }
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
                player.transform.position = seats[i].position;
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
            fps.playerCanMove = false; // Disable completely instead of just playerCanMove

        // Configure local player specifics
        if (playerNetObj.IsLocalPlayer)
        {
            player = playerNetObj.gameObject;
            playerInsideCar = true;
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
        Debug.Log($"[CarController] GetInput - playerInsideCar: {playerInsideCar}, HasDriver: {HasDriver}");
        if (playerInsideCar)
        {
            // Get local inputs
            horizontalInput = carInputHandler.SteerInput;
            verticalInput = carInputHandler.AccelerateInput;
            isBreaking = carInputHandler.BrakeInput;

            // If we're NOT the host (but we're the driver), send inputs to host
            if (!IsOwner)
            {
                Debug.Log($"[CarController] GetInput - Sending inputs to host: Steer={horizontalInput}, Accel={verticalInput}, Brake={isBreaking}");
                SendInputsToOwnerServerRpc(horizontalInput, verticalInput, isBreaking);
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
    private void SendInputsToOwnerServerRpc(float steer, float accel, bool brake)
    {
        Debug.Log($"[CLIENT] Sending van inputs to host: Steer={steer}, Accel={accel}, Brake={brake}");

        // Forward to the vehicle owner (host)
        ReceiveInputsClientRpc(steer, accel, brake,
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { OwnerClientId }
                }
            });
    }

    [ClientRpc]
    private void ReceiveInputsClientRpc(float steer, float accel, bool brake, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[CLIENT] Received van inputs from host: Steer={steer}, Accel={accel}, Brake={brake}");
        {
            // Only the host processes these
            if (!IsOwner) return;

            Debug.Log($"[HOST] Received van inputs: Steer={steer}, Accel={accel}, Brake={brake}");

            horizontalInput = steer;
            verticalInput = accel;
            isBreaking = brake;
        }
    }




    private void HandleMotor()
    {
        Debug.Log($"[CarController] HandleMotor - verticalInput: {verticalInput}, isBreaking: {isBreaking}");
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




    [ClientRpc]
    private void ExitCarClientRpc(ulong clientId, Vector3 exitPosition)
    {
        // Find the player object
        NetworkObject playerNetObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerNetObj == null) return;

        // Handle local player specific logic
        if (playerNetObj.IsLocalPlayer)
        {
            // Reset position
            playerNetObj.transform.position = exitPosition;

            // Reset car state
            playerInsideCar = false;
            player = null;
            playerCameraX = null;
            playerCameraY = null;

            // Reset input
            horizontalInput = 0;
            verticalInput = 0;
            isBreaking = false;
        }

        // Re-enable components for all clients
        if (playerNetObj.TryGetComponent<CharacterController>(out var controller))
            controller.enabled = true;

        if (playerNetObj.TryGetComponent<FirstPersonController>(out var fps))
            fps.playerCanMove = true;
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
    private void RequestExitCarServerRpc(ulong clientId, Vector3 exitPos)
    {
        // Find which seat the player is in
        int seatIndex = -1;
        for (int i = 0; i < seatOccupants.Count; i++)
        {
            if (seatOccupants[i] == clientId)
            {
                seatIndex = i;
                break;
            }
        }

        if (seatIndex == -1) return;

        // Free the seat (using 99 for empty seats to be consistent)
        seatOccupants[seatIndex] = 99;

        // Update driver status if leaving driver seat
        if (seatIndex == 0)
        {
            HasDriver = false;
        }

        // Notify all clients
        ExitCarClientRpc(clientId, exitPos);
    }



    private void ExitCar()
    {
        if (player == null) return;

        var playerNetObj = player.GetComponent<NetworkObject>();
        if (playerNetObj == null || !playerNetObj.IsLocalPlayer) return;

        RequestExitCarServerRpc(playerNetObj.OwnerClientId, exitPoint.position);
    }
}