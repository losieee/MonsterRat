using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MonsterLegless : MonoBehaviour
{
    // 몬스터 상태
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
    [SerializeField] private Transform player;

    [Header("시야")]
    [SerializeField] private float viewDistance = 12f;      // 시야 거리
    [SerializeField] private float viewAngle = 90f;         // 시야 각
    [SerializeField] private float eyeHeight = 1.6f;        // 눈 높이
    [SerializeField] private float checkInterval = 0.1f;    // 시야 체크 주기

    [Header("멈출 거리")]
    [SerializeField] private float chaseStopDistance = 1.5f;        // 플레이어 추적 시 멈출 거리
    [SerializeField] private float throwPointStopDistance = 0.5f;   // 물건 추적 시 멈출 거리

    [Header("랜덤 배회")]
    [SerializeField] private float roamingRadius = 8f;          // 랜덤 배회 반경
    [SerializeField] private float roamingStopDistance = 0.3f;     // 랜덤 목적지 도착 판정 거리
    [SerializeField] private float roamingWaitTime = 2f;           // 랜덤 위치 도착 후 대기 시간
    [SerializeField] private float roamingSampleRadius = 2f;       // NavMesh 보정 반경
    [SerializeField] private int roamingTryCount = 8;              // 랜덤 위치 찾기 시도 횟수

    [Header("대기 시간")]
    [SerializeField] private float waitAtThrowPointTime = 3f;       // 물건 위치 도착 후 대기 시간
    [SerializeField] private float gunFreezeTime = 5f;              // 총소리 대기 시간
    [SerializeField] private float lifeTime = 20f;

    private MonsterState currentState = MonsterState.Idle;

    private bool hasDetectedPlayer = false;     // 플레이어를 본적이 있는가 (true 면 시야각 상관없이 계속 추적)
    private bool isBusy = false;
    private Vector3 investigateTarget;          // 떨어진 물건 위치
    private Coroutine stateRoutine;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        agent.stoppingDistance = chaseStopDistance;
        Destroy(gameObject, lifeTime);
        StartCoroutine(VisionCheckRoutine());

        StartRoaming();
    }

    private void Update()
    {
        if (player == null)
            return;

        if (hasDetectedPlayer && !isBusy)
        {
            currentState = MonsterState.Chasing;
            agent.stoppingDistance = chaseStopDistance;
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    // 플레이어가 시야각 안에 들어왔는지 검사
    private IEnumerator VisionCheckRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);

        while (true)
        {
            if (!hasDetectedPlayer && player != null)
            {
                if (CanSeePlayer())
                {
                    hasDetectedPlayer = true;
                    currentState = MonsterState.Chasing;

                    if (stateRoutine != null)
                        StopCoroutine(stateRoutine);

                    isBusy = false;
                    agent.isStopped = false;
                    agent.stoppingDistance = chaseStopDistance;
                }
            }

            yield return wait;
        }
    }

    // 플레이어 추적
    private bool CanSeePlayer()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 playerPos = player.position + Vector3.up * 1.0f;
        Vector3 dir = playerPos - eyePos;

        if (dir.magnitude > viewDistance)
            return false;

        float angle = Vector3.Angle(transform.forward, dir.normalized);
        if (angle > viewAngle * 0.5f)
            return false;

        if (Physics.Raycast(eyePos, dir.normalized, out RaycastHit hit, viewDistance, ~0))
        {
            if (hit.transform == player)
                return true;
        }

        return false;
    }

    // 물건 확인
    public void ThrownObject(Vector3 thrownPosition)
    {
        // 가장 가까운 Navmesh 위치로 이동
        if (NavMesh.SamplePosition(thrownPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            investigateTarget = hit.position;
        }
        else
            return;

        if (stateRoutine != null)
            StopCoroutine(stateRoutine);

        stateRoutine = StartCoroutine(VisitThrowPos());
    }
    
    // 총 쐈을때 멈칫
    public void GunShot()
    {
        if (stateRoutine != null)
            StopCoroutine(stateRoutine);

        stateRoutine = StartCoroutine(GunFreeze());
    }

    // 랜덤 배회 시작
    private void StartRoaming()
    {
        if (hasDetectedPlayer) return;

        if (stateRoutine != null)
            StopCoroutine(stateRoutine);

        stateRoutine = StartCoroutine(RoamRoutine());
    }

    // 플레이어를 못 본 상태일 때 랜덤 배회
    private IEnumerator RoamRoutine()
    {
        isBusy = true;

        while (!hasDetectedPlayer)
        {
            currentState = MonsterState.Roaming;

            // 현재 위치 기준으로 랜덤 목적지 찾기
            if (TryGetRandomRoamPoint(out Vector3 roamTarget))
            {
                agent.isStopped = false;
                agent.stoppingDistance = roamingStopDistance;
                agent.SetDestination(roamTarget);

                // 경로 계산 대기
                while (agent.pathPending && !hasDetectedPlayer)
                    yield return null;

                // 목적지 도착까지 대기
                while (!hasDetectedPlayer)
                {
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                        break;

                    yield return null;
                }

                if (hasDetectedPlayer)
                    break;

                currentState = MonsterState.Idle;
                agent.isStopped = true;

                // 도착 후 잠깐 쉬었다가 다음 랜덤 위치 선택
                yield return new WaitForSeconds(roamingWaitTime);
            }
            else
            {
                // 랜덤 목적지를 못 찾았으면 잠시 대기 후 재시도
                currentState = MonsterState.Idle;
                agent.isStopped = true;
                yield return new WaitForSeconds(1f);
            }
        }

        isBusy = false;
    }

    // 랜덤 배회용 목적지 찾기
    private bool TryGetRandomRoamPoint(out Vector3 result)
    {
        for (int i = 0; i < roamingTryCount; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * roamingRadius;
            Vector3 randomPoint = transform.position + new Vector3(random2D.x, 0f, random2D.y);

            // 랜덤 좌표를 NavMesh 위 좌표로 보정
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, roamingSampleRadius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = transform.position;
        return false;
    }

    // 물건 위치로 가서 멈춤
    private IEnumerator VisitThrowPos()
    {
        isBusy = true;
        currentState = MonsterState.InvestigatingThrow;

        agent.isStopped = false;
        agent.stoppingDistance = throwPointStopDistance;

        bool success = agent.SetDestination(investigateTarget);

        while (agent.pathPending)
            yield return null;

        if (!agent.hasPath)
        {
            isBusy = false;

            if (!hasDetectedPlayer)
                StartRoaming();

            yield break;
        }

        while (true)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                break;

            yield return null;
        }

        currentState = MonsterState.WaitingAtThrowPoint;
        agent.isStopped = true;

        yield return new WaitForSeconds(waitAtThrowPointTime);

        agent.isStopped = false;
        isBusy = false;

        if (hasDetectedPlayer)
        {
            agent.stoppingDistance = chaseStopDistance;
            currentState = MonsterState.Chasing;
        }
        else
        {
            StartRoaming();
        }
    }

    // 총 쏘면 멈추는 함수
    private IEnumerator GunFreeze()
    {
        isBusy = true;
        currentState = MonsterState.StunnedByGun;

        agent.ResetPath();
        agent.isStopped = true;

        yield return new WaitForSeconds(gunFreezeTime);

        agent.isStopped = false;
        isBusy = false;

        if (hasDetectedPlayer)
        {
            agent.stoppingDistance = chaseStopDistance;
            currentState = MonsterState.Chasing;
        }
        else
        {
            StartRoaming();
        }
    }

    // 범위 체크
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Gizmos.DrawWireSphere(eyePos, viewDistance);

        Vector3 leftDir = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(eyePos, eyePos + leftDir * viewDistance);
        Gizmos.DrawLine(eyePos, eyePos + rightDir * viewDistance);
    }
}