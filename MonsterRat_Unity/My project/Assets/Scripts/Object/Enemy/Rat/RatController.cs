using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class RatController : MonoBehaviour
{
    [Header("Chase")]
    public string playerTag = "Player";
    public float stopDistance = 0.7f;
    public float repathInterval = 0.2f;

    [Header("Attack")]
    public BoxCollider hitbox;
    public float attackDistance = 1.0f;         // 공격 범위
    public float hitboxActiveTime = 0.1f;       // 히트박스 켜지는 시간
    public float attackCooldown = 0.5f;         // 공격 쿨타임
    public float attackDamage = 15f;
    public float attackAnimDuration = 0.8f;

    private string moveSpeedParam = "MoveSpeed";
    private string turnParam = "Turn";
    private string attackTrigger = "Attack";
    private string deadTrigger = "Dead";

    [Header("Animation")]
    public float walkRange = 1.2f;      // 이 거리 안으로 들어오면 걷는 애니메이션
    public float runSpeed = 3.5f;       // 멀리 있을 때
    public float walkSpeed = 1.6f;      // 가까이 있을 때

    NavMeshAgent agent;
    Animator animator;
    Transform player;
    CapsuleCollider playerCapsule;
    float repathTimer;
    bool isAttacking;
    float attackCooldownTimer;
    private float baseSpeed;
    float animMoveSpeed;
    float animTurn;
    bool isDead = false;


    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(20, 80);
        agent.stoppingDistance = stopDistance;
        agent.updateRotation = true;
        agent.updatePosition = true;
        
        agent.angularSpeed = 360f;
        agent.acceleration = 6f;
        agent.autoBraking = true;
        agent.speed = runSpeed;
        baseSpeed = runSpeed;

        if (hitbox != null)
            hitbox.enabled = false;
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        if (isDead) return;

        if (animator != null)
        {
            animator.SetFloat(moveSpeedParam, animMoveSpeed, 0.1f, Time.deltaTime);
            animator.SetFloat(turnParam, animTurn, 0.1f, Time.deltaTime);
        }

        if (player == null)
        {
            FindPlayer();
            animMoveSpeed = 0f;
            animTurn = 0f;
            return;
        }

        // 공격하는 동안 못움직이게
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        if (isAttacking)
        {
            animMoveSpeed = 0f;
            animTurn = 0f;

            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            LookAtTarget();
            return;
        }

        float surfaceDistance = GetSurfaceDistanceToPlayer();

        // 공격범위 안에 있으면 멈춤
        if (surfaceDistance <= attackDistance)
        {
            animMoveSpeed = 0f;
            animTurn = 0f;

            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            LookAtTarget();

            if (!isAttacking && attackCooldownTimer <= 0f)
            {
                StartCoroutine(ActivateHitbox());
            }
        }
        else
        {
            // 다시 추적
            if (agent.isStopped)
                agent.isStopped = false;

            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                MoveToPlayerEdge();
            }

            float currentSpeed = agent.velocity.magnitude;

            Vector3 localVel = transform.InverseTransformDirection(agent.velocity);
            float forwardSpeed = Mathf.Clamp01(Mathf.Abs(localVel.z) / Mathf.Max(baseSpeed, 0.01f));

            Vector3 toCorner = agent.steeringTarget - transform.position;
            toCorner.y = 0f;

            float signedTurn = 0f;
            float turnAngleAbs = 0f;

            float desiredBaseSpeed = surfaceDistance <= walkRange ? walkSpeed : runSpeed;

            if (toCorner.sqrMagnitude > 0.001f)
            {
                Vector3 dir = toCorner.normalized;

                signedTurn = Vector3.SignedAngle(transform.forward, dir, Vector3.up) / 90f;
                signedTurn = Mathf.Clamp(signedTurn, -1f, 1f);

                turnAngleAbs = Mathf.Abs(Vector3.SignedAngle(transform.forward, dir, Vector3.up));

                float cornerFactor = Mathf.InverseLerp(120f, 0f, turnAngleAbs);
                float targetSpeed = Mathf.Lerp(desiredBaseSpeed * 0.45f, desiredBaseSpeed, cornerFactor);

                agent.speed = Mathf.Lerp(agent.speed, targetSpeed, 6f * Time.deltaTime);
            }
            else
            {
                agent.speed = Mathf.Lerp(agent.speed, desiredBaseSpeed, 6f * Time.deltaTime);
            }

            float turnPenalty = Mathf.InverseLerp(100f, 0f, turnAngleAbs);
            float moveValue = Mathf.Clamp01(forwardSpeed * turnPenalty);

            float turnValue = 0f;
            if (currentSpeed > 0.05f)
                turnValue = signedTurn;

            animMoveSpeed = moveValue;
            animTurn = turnValue;
        }
    }

    // 쥐 사망
    public void TakeDamage()
    {
        if (isDead) return;

        isDead = true;

        animMoveSpeed = 0f;
        animTurn = 0f;
        isAttacking = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger(deadTrigger);
        }

        if (hitbox != null)
        {
            hitbox.enabled = false;
        }
    }

    // 쥐 - 사람 거리 계산
    float GetSurfaceDistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;

        float centerDistance = Vector3.Distance(transform.position, player.position);

        float playerRadius = 0.35f;
        if (playerCapsule != null)
        {
            float maxScale = Mathf.Max(player.lossyScale.x, player.lossyScale.z);
            playerRadius = playerCapsule.radius * maxScale;
        }

        float ratRadius = agent != null ? agent.radius : 0.1f;

        return Mathf.Max(0f, centerDistance - playerRadius - ratRadius);
    }

    // 플레이어 근처로 이동
    void MoveToPlayerEdge()
    {
        if (player == null || !agent.enabled) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f)
            return;

        Vector3 dir = toPlayer.normalized;

        float playerRadius = 0.35f;
        if (playerCapsule != null)
        {
            float maxScale = Mathf.Max(player.lossyScale.x, player.lossyScale.z);
            playerRadius = playerCapsule.radius * maxScale;
        }

        float ratRadius = agent.radius;
        float offset = playerRadius + ratRadius;

        Vector3 targetPos = player.position - dir * offset;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 1.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(targetPos);
        }
    }

    // 공격
    IEnumerator ActivateHitbox()
    {
        isAttacking = true;

        animMoveSpeed = 0f;
        animTurn = 0f;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
            animator.SetTrigger(attackTrigger);

        if (hitbox != null)
        {
            yield return new WaitForSeconds(0.3f);
            hitbox.enabled = true;
            CheckHitboxNow();
        }

        yield return new WaitForSeconds(hitboxActiveTime);

        if (hitbox != null)
            hitbox.enabled = false;

        yield return new WaitForSeconds(attackAnimDuration - hitboxActiveTime);

        attackCooldownTimer = attackCooldown;
        isAttacking = false;
    }

    void LookAtTarget()
    {
        if (player == null) return;

        Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookPos);
    }

    // 공격 범위에 플레이어가 있는지 없는지 확인
    void CheckHitboxNow()
    {
        if (hitbox == null) return;

        Vector3 center = hitbox.bounds.center;
        Vector3 halfExtents = hitbox.bounds.extents;
        Quaternion rotation = hitbox.transform.rotation;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation);

        foreach (Collider col in hits)
        {
            PlayerGas gas = col.GetComponent<PlayerGas>();
            PlayerHitAnim hitAnim = col.GetComponentInChildren<PlayerHitAnim>();

            if (gas != null)
                gas.AddExposure(attackDamage);
            if (hitAnim != null)
            {
                hitAnim.PlayerHit();
                break;
            }
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = null;
        playerCapsule = null;
        FindPlayer();

        animMoveSpeed = 0f;
        animTurn = 0f;
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null)
        {
            player = p.transform;
            playerCapsule = p.GetComponent<CapsuleCollider>();
        }
    }
}
