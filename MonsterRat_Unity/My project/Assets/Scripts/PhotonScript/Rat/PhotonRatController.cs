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

    // 걷는 상태 공유
    [Networked] public NetworkBool IsWalking { get; set; }

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
            IsWalking = false; // ★ 함수 대신 네트워크 변수 조작
            return;
        }

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Runner.DeltaTime;

        if (isAttacking)
        {
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

            bool walking = agent.velocity.sqrMagnitude > 0.1f && !agent.isStopped && !isAttacking;
            IsWalking = walking; 
        }
    }

    // Render로 호스트 클라 모두 동시 실행
    public override void Render()
    {
        if (animator != null && !IsDead)
        {
            animator.SetBool(walkParam, IsWalking);
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
            if (animator != null)
            {
                animator.SetBool(walkParam, false);
                animator.enabled = false;
            }

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

    // 공격 애니메이션실행
    IEnumerator ActivateHitbox()
    {
        isAttacking = true;

        // 쥐가 공격할때 방장이 클라이언트한테 공격한다고 알려주기
        Rpc_PlayAttackAnim();

        if (hitbox != null)
        {
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