using Unity.Netcode;
using UnityEngine;

public class NetworkedCarController : NetworkBehaviour
{
    [Header("Car Settings")]
    [SerializeField] private float motorForce = 2000f;
    [SerializeField] private float breakForce = 3000f;
    [SerializeField] private float maxSteerAngle = 30f;

    [Header("Seats")]
    [SerializeField] private Transform driverSeat;
    [SerializeField] private Transform[] passengerSeats;
    [SerializeField] private Transform exitPoint;

    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider[] wheelColliders;

    [Header("References")]
    [SerializeField] private Transform steeringWheel;
    [SerializeField] private float steeringWheelMaxRotation = 180f;

    private NetworkVariable<ulong> driverId = new NetworkVariable<ulong>(ulong.MaxValue);
    private NetworkList<ulong> passengerIds = new NetworkList<ulong>();
    private Rigidbody rb;

    private float horizontalInput, verticalInput;
    private bool isBreaking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            driverId.Value = ulong.MaxValue;
            passengerIds.Clear();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player") && other.TryGetComponent<NetworkObject>(out var playerNetObj))
        {
            TryEnterVehicle(playerNetObj);
        }
    }

    private void TryEnterVehicle(NetworkObject playerNetObj)
    {
        // If no driver, assign as driver
        if (driverId.Value == ulong.MaxValue)
        {
            AssignDriver(playerNetObj);
        }
        // Otherwise assign as passenger if seats available
        else if (passengerIds.Count < passengerSeats.Length)
        {
            AssignPassenger(playerNetObj);
        }
    }

    private void AssignDriver(NetworkObject playerNetObj)
    {
        driverId.Value = playerNetObj.NetworkObjectId;
        UpdatePlayerPositionClientRpc(playerNetObj.NetworkObjectId, 0);
    }

    private void AssignPassenger(NetworkObject playerNetObj)
    {
        passengerIds.Add(playerNetObj.NetworkObjectId);
        int seatIndex = passengerIds.Count - 1;
        UpdatePlayerPositionClientRpc(playerNetObj.NetworkObjectId, seatIndex + 1);
    }

    [ClientRpc]
    private void UpdatePlayerPositionClientRpc(ulong playerId, int seatIndex)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out var playerNetObj))
        {
            Transform targetSeat = seatIndex == 0 ? driverSeat : passengerSeats[seatIndex - 1];
            
            playerNetObj.transform.SetParent(targetSeat);
            playerNetObj.transform.localPosition = Vector3.zero;
            playerNetObj.transform.localRotation = Quaternion.identity;
            
            // Disable player controller
            if (playerNetObj.TryGetComponent<CharacterController>(out var cc))
                cc.enabled = false;
            
            if (playerNetObj.TryGetComponent<FirstPersonController>(out var fpc))
                fpc.playerCanMove = false;
        }
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            GetInput();
            HandleMotor();
            HandleSteering();
            UpdateWheels();
        }
    }

    private void GetInput()
    {
        if (driverId.Value == NetworkManager.LocalClientId)
        {
            // Get input only if local player is the driver
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");
            isBreaking = Input.GetKey(KeyCode.Space);
        }
        else
        {
            // Reset inputs if not driver
            horizontalInput = 0f;
            verticalInput = 0f;
            isBreaking = false;
        }
    }

    private void HandleMotor()
    {
        foreach (var wheel in wheelColliders)
        {
            wheel.motorTorque = verticalInput * motorForce;
            wheel.brakeTorque = isBreaking ? breakForce : 0f;
        }
    }

    private void HandleSteering()
    {
        float steerAngle = maxSteerAngle * horizontalInput;
        wheelColliders[0].steerAngle = steerAngle; // Front left
        wheelColliders[1].steerAngle = steerAngle; // Front right
        
        if (steeringWheel != null)
        {
            steeringWheel.localRotation = Quaternion.Euler(0f, 0f, -steerAngle * (steeringWheelMaxRotation / maxSteerAngle));
        }
    }

    private void UpdateWheels()
    {
        foreach (var wheel in wheelColliders)
        {
            if (wheel.transform.childCount > 0)
            {
                wheel.GetWorldPose(out var pos, out var rot);
                wheel.transform.GetChild(0).position = pos;
                wheel.transform.GetChild(0).rotation = rot;
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && IsOwner)
        {
            if (driverId.Value == NetworkManager.LocalClientId)
            {
                ExitVehicleServerRpc(NetworkManager.LocalClientId);
            }
        }

        if (Input.GetKeyDown(KeyCode.F) && IsOwner)
        {
            FlipVehicleServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ExitVehicleServerRpc(ulong playerId)
    {
        if (driverId.Value == playerId)
        {
            ExitVehicle(playerId, 0);
            driverId.Value = ulong.MaxValue;
        }
        else if (passengerIds.Contains(playerId))
        {
            ExitVehicle(playerId, passengerIds.IndexOf(playerId) + 1);
            passengerIds.Remove(playerId);
        }
    }

    private void ExitVehicle(ulong playerId, int seatIndex)
    {
        ExitVehicleClientRpc(playerId, seatIndex);
    }

    [ClientRpc]
    private void ExitVehicleClientRpc(ulong playerId, int seatIndex)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out var playerNetObj))
        {
            playerNetObj.transform.SetParent(null);
            playerNetObj.transform.position = exitPoint.position;
            playerNetObj.transform.rotation = exitPoint.rotation;
            
            // Re-enable player controller
            if (playerNetObj.TryGetComponent<CharacterController>(out var cc))
                cc.enabled = true;
            
            if (playerNetObj.TryGetComponent<FirstPersonController>(out var fpc))
                fpc.playerCanMove = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void FlipVehicleServerRpc()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }
}