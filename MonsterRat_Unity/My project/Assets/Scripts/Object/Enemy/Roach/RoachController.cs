using Fusion;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RoachController : NetworkBehaviour
{
    [Header("Move")]
    public float speedMin;                      // 움직임 최대 / 최소
    public float speedMax;
    public float directionChangeIntervalMin;    // 방향을 언제 바꿀지 타이밍
    public float directionChangeIntervalMax;
    public float wanderRadius = 2.5f;

    [Header("Idle")]
    public float idleChance;                    // 방향 바꾸는 타이밍에 멈출 확률
    public float idleTimeMin;                   // 멈춰있는 시간 최소 / 최대
    public float idleTimeMax;

    [Header("Pollution")]
    public NetworkPrefabRef pollutuinPreb;
    public float pollutionSpawnThreshold = 2f;

    [Header("Lifetime")]
    public float lifeTime = 20f;

    private NavMeshAgent agent;
    private Animator animator;
    private AudioSource source;
    private float timer;
    private bool isIdling;

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        source = GetComponent<AudioSource>();

        if (agent != null)
        {
            agent.speed = Random.Range(speedMin, speedMax);
            agent.angularSpeed = 720f;
            agent.acceleration = 20f;
            agent.stoppingDistance = 0f;
            agent.autoBraking = false;
        }

        if (source != null)
        {
            source.loop = true;
            source.playOnAwake = false;
        }

        if (HasStateAuthority)
        {
            StartCoroutine(PlaceAgentOnNavMeshRoutine());
        }

        PickNewDestination();

        if (Object != null && Object.HasStateAuthority)
            StartCoroutine(DespawnAfterTime());
    }

    private IEnumerator PlaceAgentOnNavMeshRoutine()
    {
        yield return null;
        yield return null;

        EnsureAgentOnNavMesh();
    }

    private void Update()
    {
        if (Object == null) return;
        if (agent == null) return;

        bool isMoving =
            !isIdling &&
            agent.isOnNavMesh &&
            !agent.pathPending &&
            agent.velocity.sqrMagnitude > 0.01f;

        float effectVolume = 1f;
        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        if (source != null)
        {
            source.loop = true;
            source.volume = effectVolume;

            if (isMoving)
            {
                if (!source.isPlaying)
                    source.Play();
            }
            else
            {
                if (source.isPlaying)
                    source.Stop();
            }
        }

        if (animator != null)
            animator.SetBool("IsMove", isMoving);

        if (!Object.HasStateAuthority) return;

        if (isIdling) return;
        if (!agent.isOnNavMesh) return;

        timer -= Time.deltaTime;

        bool reached =
            !agent.pathPending &&
            agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.1f);

        if (timer <= 0f || reached)
        {
            if (Random.value < idleChance)
                StartCoroutine(IdleAndTurn());
            else
                PickNewDestination();
        }
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (agent == null)
            return false;

        if (!agent.enabled)
            return false;

        if (!agent.gameObject.activeInHierarchy)
            return false;

        if (agent.isOnNavMesh)
            return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        return false;
    }

    // 멈췄다가 방향 바꾸기
    IEnumerator IdleAndTurn()
    {
        if (agent == null) yield break;

        isIdling = true;

        if (animator != null)
            animator.SetBool("IsMove", false);

        if (source != null && source.isPlaying)
            source.Stop();

        if (agent.isOnNavMesh)
            agent.isStopped = true;

        float t = Random.Range(idleTimeMin, idleTimeMax);
        yield return new WaitForSeconds(t);

        if (t >= pollutionSpawnThreshold && Object != null && Object.HasStateAuthority)
        {
            Runner.Spawn(pollutuinPreb, transform.position, Quaternion.identity);
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = Random.Range(speedMin, speedMax);
        }

        isIdling = false;
        PickNewDestination();
    }

    // 20초 뒤 삭제
    IEnumerator DespawnAfterTime()
    {
        yield return new WaitForSeconds(lifeTime);

        if (Object != null && Object.HasStateAuthority)
            Runner.Despawn(Object);
    }

    void PickNewDestination()
    {
        if (agent == null) return;
        if (!agent.isOnNavMesh) return;

        const int maxTry = 12;

        for (int i = 0; i < maxTry; i++)
        {
            Vector2 rand2D = Random.insideUnitCircle * wanderRadius;
            Vector3 rawTarget = transform.position + new Vector3(rand2D.x, 0f, rand2D.y);

            if (NavMesh.SamplePosition(rawTarget, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                agent.speed = Random.Range(speedMin, speedMax);
                agent.SetDestination(hit.position);
                timer = Random.Range(directionChangeIntervalMin, directionChangeIntervalMax);
                return;
            }
        }

        timer = 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
}