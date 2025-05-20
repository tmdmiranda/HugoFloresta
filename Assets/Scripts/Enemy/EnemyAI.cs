using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : NetworkBehaviour
{

    [Header("Network Sync")]
    public float networkSyncInterval = 0.1f;
    private float lastSyncTime;
    private Vector3 lastSyncedPosition;
    private bool needsInitialSync = true;
    [Header("Movement Settings")]
    public float speed = 3f;
    public float rotationSpeed = 120f;
    public float acceleration = 8f;
    public float stoppingDistance = 1f;


    [Header("Detection Settings")]
    public float detectionRange = 10f;
    public float followRefreshRate = 0.5f;

    [Header("Wander Settings")]
    public float wanderRadius = 5f;
    public float wanderTimer = 5f;
    public float minWanderDistance = 2f;

    [Header("Ground Settings")]
    public float groundCheckDistance = 0.5f;
    public LayerMask groundLayer;
    public float groundSnapSpeed = 5f;

    private NavMeshAgent agent;
    private bool isAgentReady = false;
    private Coroutine behaviorCoroutine;
    private float currentSpeed;
    private bool isChasing = false;
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();

    void Start()
    {
        if (!IsServer)
        {
            // For clients, sync position with server values
            networkPosition.OnValueChanged += OnPositionChanged;
            networkRotation.OnValueChanged += OnRotationChanged;
            return;
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent not found on enemy!");
            return;
        }

        ConfigureAgent();
        StartCoroutine(InitializeAgent());
    }


    void OnPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        if (!IsServer)
        {
            // Only update if position changed significantly to prevent jitter
            if (Vector3.Distance(transform.position, newPos) > 0.1f || needsInitialSync)
            {
                transform.position = newPos;
                needsInitialSync = false;

                // Immediately perform ground check after position update
                ClientSnapToGround();
            }
        }
    }
    void ClientSnapToGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f,
                           Vector3.down,
                           out hit,
                           groundCheckDistance + 0.5f,
                           groundLayer))
        {
            // Only adjust Y position to prevent network position conflicts
            float newY = Mathf.Lerp(transform.position.y, hit.point.y, Time.deltaTime * groundSnapSpeed);
            transform.position = new Vector3(
                transform.position.x,
                newY,
                transform.position.z
            );
        }
    }
    void Update()
    {
        if (!IsServer)
        {
            // Client-side ground snapping
            ClientSnapToGround();
            return;
        }

        // Server-side updates
        SnapToGround();

        // Network sync throttling
        if (Time.time - lastSyncTime >= networkSyncInterval)
        {
            networkPosition.Value = transform.position;
            networkRotation.Value = transform.rotation;
            lastSyncTime = Time.time;
        }
    }



    void OnRotationChanged(Quaternion oldRot, Quaternion newRot)
    {
        // Client-side rotation update
        if (!IsServer)
        {
            transform.rotation = newRot;
        }
    }

    void ConfigureAgent()
    {
        agent.speed = speed;
        agent.angularSpeed = rotationSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.updateUpAxis = false; // Important for preventing Y-axis issues
    }

    IEnumerator InitializeAgent()
    {
        int attempts = 0;
        while (!agent.isOnNavMesh && attempts < 5)
        {
            PlaceEnemyOnNavMesh();
            attempts++;
            yield return new WaitForSeconds(0.5f);
        }

        if (attempts >= 5 && !agent.isOnNavMesh)
        {
            Debug.LogError("Failed to find a valid position on NavMesh after multiple attempts.");
            yield break;
        }

        isAgentReady = true;
        behaviorCoroutine = StartCoroutine(AIBehaviorRoutine());
    }

    IEnumerator AIBehaviorRoutine()
    {
        while (isAgentReady)
        {
            GameObject nearestPlayer = FindClosestPlayer();

            if (nearestPlayer != null)
            {
                if (!isChasing)
                {
                    isChasing = true;
                    agent.autoBraking = false;
                    agent.stoppingDistance = stoppingDistance;
                }
                yield return StartCoroutine(ChasePlayer(nearestPlayer));
            }
            else
            {
                if (isChasing)
                {
                    isChasing = false;
                    agent.autoBraking = true;
                    agent.stoppingDistance = 0.1f;
                }
                yield return StartCoroutine(Wander());
            }

            // Update network variables for clients
            if (IsServer && NetworkManager.Singleton.ConnectedClientsList.Count > 0)
            {
                networkPosition.Value = transform.position;
                networkRotation.Value = transform.rotation;
            }
        }
    }

    void SnapToGround()
    {
        if (!IsServer) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, groundCheckDistance + 0.5f, groundLayer))
        {
            // Smoothly snap to ground
            Vector3 targetPosition = new Vector3(
                transform.position.x,
                hit.point.y,
                transform.position.z
            );

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * groundSnapSpeed);

            // Force NavMeshAgent to stay on ground
            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(transform.position);
            }
        }
    }

    IEnumerator ChasePlayer(GameObject player)
    {
        float lastUpdateTime = Time.time;

        while (player != null && Vector3.Distance(transform.position, player.transform.position) <= detectionRange)
        {
            if (Time.time - lastUpdateTime >= followRefreshRate)
            {
                agent.SetDestination(player.transform.position);
                lastUpdateTime = Time.time;
            }

            currentSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * 2);
            agent.speed = currentSpeed;

            yield return null;
        }
    }

    IEnumerator Wander()
    {
        Vector3 wanderPoint = RandomNavSphere(transform.position, wanderRadius, -1);

        while (Vector3.Distance(transform.position, wanderPoint) < minWanderDistance)
        {
            wanderPoint = RandomNavSphere(transform.position, wanderRadius, -1);
            yield return null;
        }

        agent.SetDestination(wanderPoint);
        float startTime = Time.time;

        currentSpeed = 0;
        float accelerateTime = 0.5f;
        float elapsedTime = 0;

        while (elapsedTime < accelerateTime)
        {
            currentSpeed = Mathf.Lerp(0, speed, elapsedTime / accelerateTime);
            agent.speed = currentSpeed;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        while (Time.time - startTime < wanderTimer &&
               agent.pathPending == false &&
               agent.remainingDistance > agent.stoppingDistance)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime);
            agent.speed = currentSpeed;
            yield return null;
        }

        elapsedTime = 0;
        float decelerateTime = 0.3f;
        float startDecelSpeed = currentSpeed;

        while (elapsedTime < decelerateTime)
        {
            currentSpeed = Mathf.Lerp(startDecelSpeed, 0, elapsedTime / decelerateTime);
            agent.speed = currentSpeed;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(Random.Range(0.2f, 0.5f));
    }

    GameObject FindClosestPlayer()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.ConnectedClientsList.Count == 0)
            return null;

        GameObject closest = null;
        float minDist = float.MaxValue;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            GameObject obj = client.PlayerObject?.gameObject;
            if (obj == null) continue;

            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < minDist && dist <= detectionRange)
            {
                minDist = dist;
                closest = obj;
            }
        }
        return closest;
    }

    Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

        return navHit.position;
    }

    void PlaceEnemyOnNavMesh()
    {
        if (agent == null) return;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            transform.position = hit.position;
        }
    }

    void OnDestroy()
    {
        if (behaviorCoroutine != null)
            StopCoroutine(behaviorCoroutine);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}
