using System.Collections;
using UnityEngine;

public class RoachController : MonoBehaviour
{
    [Header("Move")]
    public float speedMin;           // 움직임 최소 / 최대
    public float speedMax;
    public float directionChangeIntervalMin;     // 방향을 언제 바꿀지 타이밍
    public float directionChangeIntervalMax;

    [Header("Avoid Walls")]
    public float wallCheckDistance;
    public float bodyRadius = 0.2f;
    public LayerMask obstacleMask;

    [Header("Idle")]
    public float idleChance;            // 방향 바꿀 타이밍에 멈출 확률
    public float idleTimeMin;           // 멈추는 시간 최소 / 최대
    public float idleTimeMax;

    [Header("Pollution")]
    public GameObject pollutuinPreb;
    public float pollutionSpawnThreshold = 2f;

    Vector3 moveDir;
    float speed;
    float timer;
    bool isIdling;

    void Start()
    {
        PickNewDirection();

        Destroy(gameObject, 20);
    }

    private void Update()
    {
        if (isIdling) return;

        Vector3 origin = transform.position + Vector3.up * 0.05f;

        // 앞쪽 벽 감지
        if (Physics.SphereCast(origin, bodyRadius, moveDir, out RaycastHit hit, wallCheckDistance, obstacleMask))
        {
            PickNewDirection(true);
            return;
        }

        // 실제 이동
        transform.position += moveDir * speed * Time.deltaTime;

        // 회전
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        // 방향 전환 / 멈춤
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (Random.value < idleChance)
                StartCoroutine(IdleAndTurn());
            else
                PickNewDirection();
        }
    }

    // 멈췄다가 방향 바꾸기
    IEnumerator IdleAndTurn()
    {
        isIdling = true;
        float t = Random.Range(idleTimeMin, idleTimeMax);

        yield return new WaitForSeconds(t);

        if (t >= pollutionSpawnThreshold && pollutuinPreb != null)
        {
            Instantiate(pollutuinPreb, transform.position, Quaternion.identity);
        }

        isIdling = false;
        PickNewDirection();
    }

    void PickNewDirection(bool forceTurn = false)
    {
        for (int i = 0; i < 10; i++)
        {
            float angle = Random.Range(0f, 360f);
            Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

            if (forceTurn && Vector3.Dot(dir, moveDir) > 0.5f)
                dir = -dir;

            Vector3 origin = transform.position + Vector3.up * 0.1f;

            // 새 방향 앞이 바로 막혀 있으면 다른 방향 다시 뽑기
            if (Physics.SphereCast(origin, bodyRadius, dir, out _, wallCheckDistance, obstacleMask))
                continue;

            moveDir = dir;
            speed = Random.Range(speedMin, speedMax);
            timer = Random.Range(directionChangeIntervalMin, directionChangeIntervalMax);
            return;
        }

        // 방향을 못 찾았으면 잠깐 멈춤
        moveDir = Vector3.zero;
        speed = 0f;
        timer = 0.2f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawWireSphere(origin, bodyRadius);
        Gizmos.DrawLine(origin, origin + moveDir.normalized * wallCheckDistance);
    }
}