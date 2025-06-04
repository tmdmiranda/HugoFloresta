using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3f; // Velocidade de movimento do inimigo
    public float rotationSpeed = 120f; // Velocidade de rotação do inimigo
    public float acceleration = 8f; // Aceleração do inimigo ao iniciar movimento
    public float stoppingDistance = 1f; // Distância mínima para parar ao aproximar-se do alvo

    [Header("Detection Settings")]
    public float detectionRange = 10f; // Raio de deteção para encontrar jogadores
    public float followRefreshRate = 0.5f; // Intervalo de atualização ao seguir o jogador
    public LayerMask obstacleLayerMask = -1; // Máscara de camadas que bloqueiam a linha de visão

    [Header("Wander Settings")]
    public float wanderRadius = 5f; // Raio máximo para escolher um ponto aleatório para patrulhar
    public float wanderTimer = 5f; // Tempo máximo a patrulhar antes de escolher novo destino
    public float minWanderDistance = 2f; // Distância mínima para garantir que o ponto de patrulha não é demasiado próximo

    private NavMeshAgent agent; // Referência ao componente de navegação do Unity
    private bool isAgentReady = false; // Indica se o agente está pronto para ser usado
    private Coroutine behaviorCoroutine; // Referência à corrotina principal de comportamento
    private float currentSpeed; // Velocidade atual do inimigo (usada para transições suaves)
    private bool isChasing = false; // Indica se o inimigo está atualmente a perseguir um jogador

    [Header("animation")]
    [SerializeField] private Animator animator; // Referência ao componente Animator para animações

    void Start()
    {
        // Se não for o servidor, desativa o script
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        // Obtém o componente NavMeshAgent associado ao inimigo
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent not found on enemy!");
            return;
        }

        // Configura o agente e inicia a corrotina de inicialização
        ConfigureAgent();
        StartCoroutine(InitializeAgent());
    }

    void Update()
    {
        // Só o servidor executa a lógica de movimento
        if (!IsServer) return;

        // definir valores de animação
        float animSpeed = (agent != null && agent.isOnNavMesh) ? agent.velocity.magnitude : 0f;
        bool isWalking = animSpeed > 0.1f;
        bool isIdle = !isWalking;
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isIdle", isIdle);
    }

    // Configura os parâmetros do NavMeshAgent conforme as definições públicas
    void ConfigureAgent()
    {
        agent.speed = speed;
        agent.angularSpeed = rotationSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.updateUpAxis = false;
    }

    // Corrotina que tenta colocar o inimigo numa posição válida do NavMesh
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

    // Corrotina principal que gere o comportamento do inimigo (perseguir ou patrulhar)
    IEnumerator AIBehaviorRoutine()
    {
        while (isAgentReady && agent != null && agent.isOnNavMesh)
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

            yield return new WaitForSeconds(0.1f);
        }

        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogError("Enemy AI stopped: NavMeshAgent is null or not on NavMesh");
        }
    }

    // Corrotina que faz o inimigo perseguir o jogador enquanto este estiver ao alcance
    IEnumerator ChasePlayer(GameObject player)
    {
        float lastUpdateTime = Time.time;

        while (player != null && agent != null && agent.isOnNavMesh &&
               Vector3.Distance(transform.position, player.transform.position) <= detectionRange &&
               HasLineOfSight(player))
        {
            // Atualiza o destino do agente a cada intervalo definido
            if (Time.time - lastUpdateTime >= followRefreshRate)
            {
                if (agent.isActiveAndEnabled)
                {
                    agent.SetDestination(player.transform.position);
                }
                lastUpdateTime = Time.time;
            }

            // Faz a transição suave da velocidade
            currentSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * 2);
            if (agent.isActiveAndEnabled)
            {
                agent.speed = currentSpeed;
            }

            yield return null;
        }
    }

    // Corrotina que faz o inimigo patrulhar/andar aleatoriamente
    IEnumerator Wander()
    {
        if (agent == null || !agent.isOnNavMesh) yield break;

        // Escolhe um ponto aleatório válido
        Vector3 wanderPoint = RandomNavSphere(transform.position, wanderRadius, -1);

        // Garante que o ponto não é demasiado próximo
        while (Vector3.Distance(transform.position, wanderPoint) < minWanderDistance)
        {
            wanderPoint = RandomNavSphere(transform.position, wanderRadius, -1);
            yield return null;
        }

        // Define o destino do agente
        if (agent.isActiveAndEnabled)
        {
            agent.SetDestination(wanderPoint);
        }
        float startTime = Time.time;

        // Acelera gradualmente
        currentSpeed = 0;
        float accelerateTime = 0.5f;
        float elapsedTime = 0;

        while (elapsedTime < accelerateTime && agent != null && agent.isActiveAndEnabled)
        {
            currentSpeed = Mathf.Lerp(0, speed, elapsedTime / accelerateTime);
            agent.speed = currentSpeed;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Mantém a velocidade enquanto patrulha
        while (agent != null && agent.isActiveAndEnabled &&
               Time.time - startTime < wanderTimer &&
               agent.pathPending == false &&
               agent.remainingDistance > agent.stoppingDistance)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime);
            agent.speed = currentSpeed;
            yield return null;
        }

        // Desacelera gradualmente
        elapsedTime = 0;
        float decelerateTime = 0.3f;
        float startDecelSpeed = currentSpeed;

        while (elapsedTime < decelerateTime && agent != null && agent.isActiveAndEnabled)
        {
            currentSpeed = Mathf.Lerp(startDecelSpeed, 0, elapsedTime / decelerateTime);
            agent.speed = currentSpeed;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Espera um tempo aleatório antes de escolher novo destino
        yield return new WaitForSeconds(Random.Range(0.2f, 0.5f));
    }

    // Verifica se existe linha de visão direta para o alvo (sem obstáculos)
    bool HasLineOfSight(GameObject target)
    {
        if (target == null) return false;

        Vector3 directionToTarget = target.transform.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        // Lança um raio do inimigo até ao jogador para verificar obstáculos
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToTarget.normalized, out hit, distanceToTarget, obstacleLayerMask))
        {
            // Se o raio atingir algo diferente do alvo, a linha de visão está bloqueada
            return hit.collider.gameObject == target;
        }

        // Sem obstáculos, linha de visão está livre
        return true;
    }

    // Procura o jogador mais próximo que esteja ao alcance e visível
    GameObject FindClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0)
            return null;

        GameObject closest = null;
        float minDist = float.MaxValue;
        foreach (var obj in players)
        {
            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < minDist && dist <= detectionRange && HasLineOfSight(obj))
            {
                minDist = dist;
                closest = obj;
            }
        }
        return closest;
    }

    // Gera um ponto aleatório válido no NavMesh dentro de um raio
    Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

        return navHit.position;
    }

    // Tenta colocar o inimigo numa posição válida do NavMesh
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

    // Método chamado quando o inimigo é destruído (por exemplo: morre)
    new void OnDestroy()
    {
        if (behaviorCoroutine != null)
            StopCoroutine(behaviorCoroutine);
    }

    // Desenha gizmos no editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange); // Raio de deteção

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, wanderRadius); // Raio de patrulha

        /*Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);*/
    }
}