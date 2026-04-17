using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Fusion; // Fusion 네임스페이스 추가

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

    private string walkParam = "isWalking";
    private string attackTrigger = "Attack";

    NavMeshAgent agent;
    Animator animator;
    Transform targetPlayer;
    CapsuleCollider playerCapsule;
    float repathTimer;
    bool isAttacking;
    float attackCooldownTimer;

    [Networked, OnChangedRender(nameof(OnDeadStateChanged))]
    public NetworkBool IsDead { get; set; }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        agent.stoppingDistance = stopDistance;
        agent.updateRotation = true;
        agent.updatePosition = true;
        agent.angularSpeed = 360f;
        agent.acceleration = 20f;

        if (hitbox != null)
            hitbox.enabled = false;
    }

    public override void Spawned()
    {

        //IsDead = false;
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
            SetWalking(false);
            return;
        }

        // 공격 쿨타임 감소
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Runner.DeltaTime;

        if (isAttacking)
        {
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            SetWalking(false);
            LookAtTarget();
            return;
        }

        float surfaceDistance = GetSurfaceDistanceToPlayer();

        // 공격 범위 진입 시
        if (surfaceDistance <= attackDistance)
        {
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            SetWalking(false);
            LookAtTarget();

            if (!isAttacking && attackCooldownTimer <= 0f)
            {
                StartCoroutine(ActivateHitbox());
            }
        }
        else
        {
            // 추적 로직
            if (agent.isStopped) agent.isStopped = false;

            repathTimer -= Runner.DeltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                MoveToPlayerEdge();
            }

            bool walking = agent.velocity.sqrMagnitude > 0.1f && !agent.isStopped && !isAttacking;
            SetWalking(walking);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_TakeDamage(Vector3 hitPoint)
    {
        if (IsDead) return;
        IsDead = true;
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
            if (agent != null) agent.enabled = false;
            if (animator != null) animator.enabled = false;

            Rigidbody rb = GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                rb.isKinematic = !HasStateAuthority;
                rb.freezeRotation = false;
            }

            SetLayerRecursively(gameObject, 3);
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

    void SetWalking(bool walking)
    {
        if (animator != null) animator.SetBool(walkParam, walking);
    }

    IEnumerator ActivateHitbox()
    {
        isAttacking = true;

        if (hitbox != null)
        {
            hitbox.enabled = true;
            CheckHitboxNow();
        }

        yield return new WaitForSeconds(hitboxActiveTime);

        if(hitbox != null)
            hitbox.enabled = false;

        yield return new WaitForSeconds(attackAnimDuration - hitboxActiveTime);
        attackCooldownTimer = attackCooldown;
        isAttacking = false;
    }

    // 가장 가까운 플레이어 찾기
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

    // 플레이어가 Hitbox 안에 있는지 확인 
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
        float desiredGap = stopDistance;

        float offset = playerRadius + ratRadius + desiredGap;

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