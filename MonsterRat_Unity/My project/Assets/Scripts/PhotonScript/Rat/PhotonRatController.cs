using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Fusion;

public class PhotonRatController : NetworkBehaviour
{
    [Header("Chase")]
    public string playerTag = "Player";
    public float stopDistance = 0.7f;
    public float repathInterval = 0.2f;

    [Header("Attack")]
    public BoxCollider hitbox;
    public float attackDistance = 1.0f;
    public float hitboxActiveTime = 0.1f;
    public float attackCooldown = 0.5f;
    public float attackDamage = 15f;
    public float attackAnimDuration = 0.8f;

    [Header("Dead Effects")]
    public GameObject bloodPreb;
    public LayerMask groundMask;

    [Header("Animation")]
    public float walkRange = 1.2f;      // 이 거리 안으로 들어오면 걷는 애니메이션
    public float runSpeed = 3.5f;       // 멀리 있을 때
    public float walkSpeed = 1.6f;      // 가까이 있을 때
    [Networked] public float NetMoveSpeed { get; set; }

    [Networked] public float NetTurn { get; set; }
    private float baseSpeed;

    private string moveSpeedParam = "MoveSpeed";
    private string turnParam = "Turn";
    private string attackTrigger = "Attack";
    private string deadTrigger = "Dead";

    NavMeshAgent agent;
    Animator animator;
    Transform targetPlayer;
    CapsuleCollider playerCapsule;
    float repathTimer;
    bool isAttacking;
    float attackCooldownTimer;

    [Networked, OnChangedRender(nameof(OnDeadStateChanged))]
    public NetworkBool IsDead { get; set; }

    // 걷는 상태 공유
    [Networked] public NetworkBool IsWalking { get; set; }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = UnityEngine.Random.Range(20, 80);
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

    public override void Spawned()
    {
        FindPlayer();
        if (IsDead)
        {
            OnDeadStateChanged();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsDead || !HasStateAuthority) return;

        FindPlayer();

        if (targetPlayer == null)
        {
            NetMoveSpeed = 0f;
            NetTurn = 0f;
            IsWalking = false;
            return;
        }

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Runner.DeltaTime;

        if (isAttacking)
        {
            NetMoveSpeed = 0f;
            NetTurn = 0f;

            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            IsWalking = false; // 이거 바꿨어요
            LookAtTarget();
            return;
        }

        float surfaceDistance = GetSurfaceDistanceToPlayer();

        if (surfaceDistance <= attackDistance)
        {
            NetMoveSpeed = 0f;
            NetTurn = 0f;

            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            IsWalking = false; // 이거 바꿈 
            LookAtTarget();

            if (!isAttacking && attackCooldownTimer <= 0f)
            {
                StartCoroutine(ActivateHitbox());
            }
        }
        else
        {
            if (agent.isStopped) agent.isStopped = false;

            repathTimer -= Runner.DeltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                MoveToPlayerEdge();
            }

            float currentSpeed = agent.velocity.magnitude;

            // 로컬 기준 속도
            Vector3 localVel = transform.InverseTransformDirection(agent.velocity);
            float forwardSpeed = Mathf.Clamp01(Mathf.Abs(localVel.z) / Mathf.Max(baseSpeed, 0.01f));

            // 다음 경로 방향 기준 회전량 계산
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

                // 코너일수록 감속
                float cornerFactor = Mathf.InverseLerp(120f, 0f, turnAngleAbs);
                float targetSpeed = Mathf.Lerp(desiredBaseSpeed * 0.45f, desiredBaseSpeed, cornerFactor);

                agent.speed = Mathf.Lerp(agent.speed, targetSpeed, 6f * Runner.DeltaTime);
            }
            else
            {
                agent.speed = Mathf.Lerp(agent.speed, desiredBaseSpeed, 6f * Runner.DeltaTime);
            }

            // 애니메이션용 이동값
            float turnPenalty = Mathf.InverseLerp(100f, 0f, turnAngleAbs);
            float animMove = Mathf.Clamp01(forwardSpeed * turnPenalty);

            // 애니메이션용 회전값
            float animTurn = 0f;
            if (currentSpeed > 0.05f)
            {
                animTurn = signedTurn;
            }

            NetMoveSpeed = animMove;
            NetTurn = animTurn;

            bool walking = currentSpeed > 0.05f && !agent.isStopped && !isAttacking;
            IsWalking = walking;
        }
    }

    // Render로 호스트 클라 모두 동시 실행
    public override void Render()
    {
        if (animator != null && !IsDead)
        {
            animator.SetFloat(moveSpeedParam, NetMoveSpeed, 0.1f, Time.deltaTime);
            animator.SetFloat(turnParam, NetTurn, 0.1f, Time.deltaTime);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_TakeDamage(Vector3 hitPoint)
    {
        if (IsDead) return;
        IsDead = true;

        StartCoroutine(EnableDeadPhysicsDelayed());

        if (bloodPreb != null)
        {
            Vector3 start = transform.position + Vector3.up * 0.3f;
            if (Physics.Raycast(start, Vector3.down, out RaycastHit groundHit, 5f, groundMask, QueryTriggerInteraction.Ignore))
            {
                Runner.Spawn(bloodPreb, groundHit.point, Quaternion.FromToRotation(Vector3.up, groundHit.normal));
            }
            else
            {
                Runner.Spawn(bloodPreb, transform.position, Quaternion.identity);
            }
        }
    }

    private void OnDeadStateChanged()
    {
        if (IsDead)
        {
            if (animator != null)
            {
                animator.SetTrigger(deadTrigger);
            }

            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.enabled = false;
            }

            Rigidbody rb = GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            SetLayerRecursively(gameObject, 3);
        }
    }

    private IEnumerator EnableDeadPhysicsDelayed()
    {
        yield return new WaitForSeconds(1f);

        Rigidbody rb = GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.freezeRotation = false;
        }
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        for (int i = 0; i < obj.transform.childCount; i++)
            SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
    }

    void LookAtTarget()
    {
        Vector3 lookPos = new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z);
        transform.LookAt(lookPos);
    }

    // 공격 애니메이션실행
    IEnumerator ActivateHitbox()
    {
        isAttacking = true;

        // 쥐가 공격할때 방장이 클라이언트한테 공격한다고 알려주기
        Rpc_PlayAttackAnim();

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

    //호스트 클라 모두 공격 애니메이션 볼 수 있게
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlayAttackAnim()
    {
        if (animator != null && !IsDead)
        {
            animator.SetTrigger(attackTrigger);
        }
    }

    void FindPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);

        Transform closest = null;
        CapsuleCollider closestCapsule = null;
        float closestSqrDist = Mathf.Infinity;

        Vector3 myPos = transform.position;

        foreach (GameObject p in players)
        {
            if (p == null || !p.activeInHierarchy)
                continue;

            float sqrDist = (p.transform.position - myPos).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closest = p.transform;
                closestCapsule = p.GetComponent<CapsuleCollider>();
            }
        }

        targetPlayer = closest;
        playerCapsule = closestCapsule;
    }

    float GetSurfaceDistanceToPlayer()
    {
        if (targetPlayer == null) return Mathf.Infinity;

        float centerDistance = Vector3.Distance(transform.position, targetPlayer.position);

        float playerRadius = 0.35f;
        if (playerCapsule != null)
        {
            float maxScale = Mathf.Max(targetPlayer.lossyScale.x, targetPlayer.lossyScale.z);
            playerRadius = playerCapsule.radius * maxScale;
        }

        float ratRadius = agent != null ? agent.radius : 0.1f;

        return Mathf.Max(0f, centerDistance - playerRadius - ratRadius);
    }

    void CheckHitboxNow()
    {
        if (hitbox == null) return;

        Vector3 center = hitbox.bounds.center;
        Vector3 halfExtents = hitbox.bounds.extents;
        Quaternion rotation = hitbox.transform.rotation;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation);

        foreach (Collider col in hits)
        {
            HandOverDamage receiver = col.GetComponent<HandOverDamage>();
            if (receiver != null)
            {
                receiver.Rpc_TakeRatHit(attackDamage);
                break;
            }
        }
    }

    void MoveToPlayerEdge()
    {
        if (targetPlayer == null || !agent.enabled) return;

        Vector3 toPlayer = targetPlayer.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f)
            return;

        Vector3 dir = toPlayer.normalized;

        float playerRadius = 0.35f;
        if (playerCapsule != null)
        {
            float maxScale = Mathf.Max(targetPlayer.lossyScale.x, targetPlayer.lossyScale.z);
            playerRadius = playerCapsule.radius * maxScale;
        }

        float ratRadius = agent.radius;
        float offset = playerRadius + ratRadius;

        Vector3 targetPos = targetPlayer.position - dir * offset;

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
}