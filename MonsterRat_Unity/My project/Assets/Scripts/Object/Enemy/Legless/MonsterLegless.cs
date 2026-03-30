using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class MonsterLegless : NetworkBehaviour
{
    public enum MonsterState
    {
        Idle,
        Roaming,
        Chasing,
        InvestigatingThrow,
        StunnedByGun,
        WaitingAtThrowPoint
    }

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator anim;

    [Header("Player Search")]
    [SerializeField] private string playerTag = "Player";

    [Header("시야")]
    [SerializeField] private float viewDistance = 12f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private float checkInterval = 0.1f;

    [Header("멈출 거리")]
    [SerializeField] private float chaseStopDistance = 1.5f;
    [SerializeField] private float throwPointStopDistance = 0.5f;

    [Header("랜덤 배회")]
    [SerializeField] private float roamingRadius = 8f;
    [SerializeField] private float roamingStopDistance = 0.3f;
    [SerializeField] private float roamingWaitTime = 2f;
    [SerializeField] private float roamingSampleRadius = 2f;
    [SerializeField] private int roamingTryCount = 8;

    [Header("대기 시간")]
    [SerializeField] private float waitAtThrowPointTime = 3f;
    [SerializeField] private float gunFreezeTime = 5f;
    [SerializeField] private float lifeTime = 20f;

    [Header("애니메이션")]
    [SerializeField] private string isMovingParam = "IsMoving";
    [SerializeField] private float movingThreshold = 0.05f;
    [SerializeField] private float animDampSpeed = 10f;
    
    [Networked] private MonsterState NetState { get; set; }             // 현재 몬스터 상태
    [Networked] private NetworkBool HasDetectedPlayer { get; set; }     // 플레이어를 포착한적이 있는지
    [Networked] private NetworkBool IsBusy { get; set; }                // 현재 무슨 동작을 하고있는지
    [Networked] private NetworkBool IsMovingNet { get; set; }           // 이동 애니메이션 동기화용

    private Transform currentTarget;
    private Vector3 investigateTarget;
    private Coroutine stateRoutine;
    private Coroutine visionRoutine;
    private Coroutine lifeRoutine;
    private bool localFallbackMode;
    private bool overrideInvestigation;
    private bool resumeChaseAfterInvestigation;

    // 프록시 화면에서 bool 전환이 너무 딱딱해 보이는 걸 조금 완화
    private float movingBlendVisual;

    // RPC - 누구든 호출 가능한 함수, 실행은 몬스터에서만 실행 됨
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_InvestigatePoint(Vector3 point)
    {
        Debug.Log($"[Monster] RPC_InvestigatePoint: {point}");
        ThrownObject(point);
    }

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (anim == null) anim = GetComponent<Animator>();
    }

    private void Start()
    {
        
    }

    // 스폰 됐을때
    public override void Spawned()
    {
        InitializeMonster();

        if (HasStateAuthority)
            BeginMonsterLogic();
    }

    // agent 기본 상태 세팅
    private void InitializeMonster()
    {
        if (agent != null && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.stoppingDistance = chaseStopDistance;
        }
    }

    // 몬스터 로직 시작
    private void BeginMonsterLogic()
    {
        if (lifeRoutine != null) StopCoroutine(lifeRoutine);
        lifeRoutine = StartCoroutine(LifeRoutine());

        if (visionRoutine != null) StopCoroutine(visionRoutine);
        visionRoutine = StartCoroutine(VisionCheckRoutine());

        StartRoaming();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        TickAuthority();
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid)
            return;

        if (localFallbackMode)
            TickAuthority();

        UpdateAnimationVisual();
    }

    // 플레이어 추적 / 이동 애니메이션 네트워크 값 갱신
    private void TickAuthority()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        // 박스, 총 소리 중 플레이어 추적 금지
        if (!overrideInvestigation && currentTarget != null && HasDetectedPlayer && !IsBusy)
        {
            NetState = MonsterState.Chasing;
            agent.stoppingDistance = chaseStopDistance;
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
        }

        float speed = agent.velocity.magnitude;
        IsMovingNet = speed > movingThreshold;
    }

    // 몬스터 생존시간
    private IEnumerator LifeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);

        if (Object != null && HasStateAuthority)
            Runner.Despawn(Object);
        else
            Destroy(gameObject);
    }

    // 일정 주기로 시야 체크
    private IEnumerator VisionCheckRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);

        while (true)
        {
            // 타깃이 없으면 새타깃 탐색
            if (currentTarget == null)
            {
                Transform visibleTarget = FindClosestVisiblePlayer();

                if (visibleTarget != null)
                {
                    currentTarget = visibleTarget;
                    HasDetectedPlayer = true;
                    NetState = MonsterState.Chasing;

                    if (stateRoutine != null)
                        StopCoroutine(stateRoutine);

                    IsBusy = false;
                    agent.isStopped = false;
                    agent.stoppingDistance = chaseStopDistance;
                }
            }
            else
            {
                // 추적 대상 사라지면 다시 랜덤 배회
                if (!currentTarget.gameObject.activeInHierarchy)
                {
                    currentTarget = null;
                    HasDetectedPlayer = false;

                    if (!IsBusy)
                        StartRoaming();
                }
            }

            yield return wait;
        }
    }

    // 시야 안에 들어온 플레이어 중 가장 가까운 플레이어 추적
    private Transform FindClosestVisiblePlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);

        Transform best = null;
        float bestSqr = float.MaxValue;

        foreach (GameObject go in players)
        {
            if (go == null) continue;

            Transform target = go.transform;

            if (!CanSeePlayer(target))
                continue;

            float sqr = (target.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = target;
            }
        }

        return best;
    }

    // 플레이어 탐색
    private bool CanSeePlayer(Transform target)
    {
        if (target == null)
            return false;

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 playerPos = target.position + Vector3.up * 1.0f;
        Vector3 dir = playerPos - eyePos;

        if (dir.magnitude > viewDistance)
            return false;

        float angle = Vector3.Angle(transform.forward, dir.normalized);
        if (angle > viewAngle * 0.5f)
            return false;

        if (Physics.Raycast(eyePos, dir.normalized, out RaycastHit hit, viewDistance, ~0))
            return hit.transform == target || hit.transform.IsChildOf(target);

        return false;
    }

    // 박스가 던져졌을 때 그 자리 탐색
    public void ThrownObject(Vector3 thrownPosition)
    {
        if (!CanRunAuthorityLogic())
        {
            return;
        }

        if (!NavMesh.SamplePosition(thrownPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            return;
        }

        investigateTarget = hit.position;
        resumeChaseAfterInvestigation = currentTarget != null && HasDetectedPlayer;
        overrideInvestigation = true;
        IsBusy = true;

        if (stateRoutine != null)
            StopCoroutine(stateRoutine);

        stateRoutine = StartCoroutine(VisitThrowPos(false));
    }

    // 총 쏜자리 탐색
    public void GunShot(Vector3 gunShotPosition)
    {
        if (!CanRunAuthorityLogic())
            return;

        if (!NavMesh.SamplePosition(gunShotPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            return;

        investigateTarget = hit.position;

        resumeChaseAfterInvestigation = currentTarget != null && HasDetectedPlayer;

        overrideInvestigation = true;
        IsBusy = true;

        if (stateRoutine != null)
            StopCoroutine(stateRoutine);

        stateRoutine = StartCoroutine(VisitThrowPos(true));
    }

    private bool CanRunAuthorityLogic()
    {
        if (localFallbackMode)
            return true;

        return HasStateAuthority;
    }

    // 랜덤 배회
    private void StartRoaming()
    {
        if (HasDetectedPlayer)
            return;

        if (stateRoutine != null)
            StopCoroutine(stateRoutine);

        stateRoutine = StartCoroutine(RoamRoutine());
    }

    private IEnumerator RoamRoutine()
    {
        IsBusy = true;

        while (!HasDetectedPlayer)
        {
            NetState = MonsterState.Roaming;

            if (TryGetRandomRoamPoint(out Vector3 roamTarget))
            {
                agent.isStopped = false;
                agent.stoppingDistance = roamingStopDistance;
                agent.SetDestination(roamTarget);

                while (agent.pathPending && !HasDetectedPlayer)
                    yield return null;

                while (!HasDetectedPlayer)
                {
                    bool arrived =
                        !agent.pathPending &&
                        (
                            !agent.hasPath ||
                            agent.remainingDistance <= agent.stoppingDistance + 0.05f ||
                            agent.pathStatus == NavMeshPathStatus.PathInvalid
                        );

                    if (arrived)
                        break;

                    yield return null;
                }

                if (HasDetectedPlayer)
                    break;

                NetState = MonsterState.Idle;
                agent.isStopped = true;
                yield return new WaitForSeconds(roamingWaitTime);
            }
            else
            {
                NetState = MonsterState.Idle;
                agent.isStopped = true;
                yield return new WaitForSeconds(1f);
            }
        }

        IsBusy = false;
    }

    // NavMesh 위의 랜덤 목적지 하나 찾는 함수
    private bool TryGetRandomRoamPoint(out Vector3 result)
    {
        Vector3 center = transform.position;

        for (int i = 0; i < roamingTryCount; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * roamingRadius;
            Vector3 randomPoint = center + new Vector3(random2D.x, 0f, random2D.y);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, roamingSampleRadius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = center;
        return false;
    }

    // 박스 / 총소리 위치 탐색
    private IEnumerator VisitThrowPos(bool fromGunShot)
    {
        IsBusy = true;
        NetState = fromGunShot ? MonsterState.StunnedByGun : MonsterState.InvestigatingThrow;

        // 혹시 이전 경로가 남아있으면 초기화
        agent.ResetPath();
        agent.isStopped = false;
        agent.stoppingDistance = throwPointStopDistance;
        agent.SetDestination(investigateTarget);

        while (agent.pathPending)
            yield return null;

        if (!agent.hasPath)
        {
            overrideInvestigation = false;
            IsBusy = false;

            if (resumeChaseAfterInvestigation && currentTarget != null && HasDetectedPlayer)
            {
                resumeChaseAfterInvestigation = false;
                NetState = MonsterState.Chasing;
            }
            else
            {
                resumeChaseAfterInvestigation = false;
                agent.ResetPath();
                agent.isStopped = false;
                NetState = MonsterState.Idle;
                stateRoutine = StartCoroutine(RoamRoutine());
            }

            yield break;
        }

        while (true)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
                break;

            yield return null;
        }

        NetState = MonsterState.WaitingAtThrowPoint;
        agent.isStopped = true;

        yield return new WaitForSeconds(fromGunShot ? gunFreezeTime : waitAtThrowPointTime);

        agent.isStopped = false;

        // 조사 종료
        overrideInvestigation = false;
        IsBusy = false;

        if (resumeChaseAfterInvestigation && currentTarget != null && HasDetectedPlayer)
        {
            resumeChaseAfterInvestigation = false;
            NetState = MonsterState.Chasing;
        }
        else
        {
            resumeChaseAfterInvestigation = false;
            agent.ResetPath();
            agent.isStopped = false;
            NetState = MonsterState.Idle;
            stateRoutine = StartCoroutine(RoamRoutine());
        }
    }

    // 멀티 애니메이션 부드럽게
    private void UpdateAnimationVisual()
    {
        if (anim == null)
            return;

        // 아직 Fusion Spawn 전이면 Networked 값 접근 금지
        if (Object == null || !Object.IsValid)
            return;

        float target = IsMovingNet ? 1f : 0f;
        movingBlendVisual = Mathf.Lerp(movingBlendVisual, target, Time.deltaTime * animDampSpeed);

        bool visualMoving = movingBlendVisual > 0.5f;
        anim.SetBool(isMovingParam, visualMoving);
    }
}