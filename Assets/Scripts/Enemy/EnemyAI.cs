using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : NetworkBehaviour
{
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

    private NavMeshAgent agent;
    private bool isAgentReady = false;
    private Coroutine behaviorCoroutine;
    private float currentSpeed;
    private Vector3 lastDirection;
    private bool isChasing = false;

    void Start()
    {
        if (!IsServer)
        {
            enabled = false;
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

    void ConfigureAgent()
    {
        agent.speed = speed;
        agent.angularSpeed = rotationSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = false; // Disable auto-braking for smoother transitions
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
                if (!isChasing) // Only restart coroutine if we weren't already chasing
                {
                    isChasing = true;
                    agent.autoBraking = false; // Smoother transitions when chasing
                    agent.stoppingDistance = stoppingDistance;
                }
                yield return StartCoroutine(ChasePlayer(nearestPlayer));
            }
            else
            {
                if (isChasing) // Only restart coroutine if we were chasing before
                {
                    isChasing = false;
                    agent.autoBraking = true; // Better for wandering
                    agent.stoppingDistance = 0.1f; // Get closer to wander points
                }
                yield return StartCoroutine(Wander());
            }
        }
    }

    IEnumerator ChasePlayer(GameObject player)
    {
        float lastUpdateTime = Time.time;
        
        while (player != null && Vector3.Distance(transform.position, player.transform.position) <= detectionRange)
        {
            // Only update path at followRefreshRate intervals for performance
            if (Time.time - lastUpdateTime >= followRefreshRate)
            {
                agent.SetDestination(player.transform.position);
                lastUpdateTime = Time.time;
            }

            // Smooth speed adjustment
            currentSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * 2);
            agent.speed = currentSpeed;

            yield return null;
        }
    }

    IEnumerator Wander()
    {
        Vector3 wanderPoint = RandomNavSphere(transform.position, wanderRadius, -1);
        
        // Ensure the new position is far enough to be worth moving to
        while (Vector3.Distance(transform.position, wanderPoint) < minWanderDistance)
        {
            wanderPoint = RandomNavSphere(transform.position, wanderRadius, -1);
            yield return null;
        }

        agent.SetDestination(wanderPoint);
        float startTime = Time.time;

        // Smooth acceleration when starting to wander
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

        // Continue wandering until reached destination or timer expires
        while (Time.time - startTime < wanderTimer && 
               agent.pathPending == false && 
               agent.remainingDistance > agent.stoppingDistance)
        {
            // Smooth speed maintenance
            currentSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime);
            agent.speed = currentSpeed;
            yield return null;
        }

        // Brief pause between wanders with smooth deceleration
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
    }
}