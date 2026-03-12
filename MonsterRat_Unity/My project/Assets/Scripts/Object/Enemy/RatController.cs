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

    private string walkParam = "isWalking";
    private string attackTrigger = "Attack";

    NavMeshAgent agent;
    Animator animator;
    Transform player;
    float repathTimer;
    bool isAttacking;
    float attackCooldownTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.stoppingDistance = stopDistance;
        agent.updateRotation = true;
        agent.updatePosition = true;

        if (hitbox != null)
            hitbox.gameObject.SetActive(false);
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            SetWalking(false);
            return;
        }

        // 나중에 공격하는 애니메이션 만들면 공격하는 동안 못움직이게

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        float distance = Vector3.Distance(transform.position, player.position);
        // 공격범위 안에 있으면 멈춤
        if (distance <= attackDistance)
        {
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            SetWalking(false);

            Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.LookAt(lookPos);

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
                agent.SetDestination(player.position);
            }

            bool walking = agent.velocity.sqrMagnitude > 0.1f && !agent.isStopped && !isAttacking;
            SetWalking(walking);
        }
    }

    // 공격
    IEnumerator ActivateHitbox()
    {
        isAttacking = true;
        SetWalking(false);

        if (hitbox != null)
        {
            hitbox.gameObject.SetActive(true);

            CheckHitboxNow();
        }

        yield return new WaitForSeconds(hitboxActiveTime);

        if (hitbox != null)
            hitbox.gameObject.SetActive(false);

        attackCooldownTimer = attackCooldown;
        isAttacking = false;
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
            if (gas != null)
            {
                gas.AddExposure(attackDamage);
                break;
            }
        }
    }

    void SetWalking(bool walking)
    {
        if (animator != null)
            animator.SetBool(walkParam, walking);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = null;
        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;    
    }
}
