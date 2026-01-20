using System.Collections;
using UnityEngine;

public class RatController : MonoBehaviour
{
    [Header("Move")]
    public float speedMin;           // 움직임 최소 / 최대
    public float speedMax;
    public float directionChangeIntervalMin;     // 방향을 언제 바꿀지 타이밍
    public float directionChangeIntervalMax;

    [Header("Avoid Walls")]
    public float wallCheckDistance;
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
    }

    void Update()
    {
        if (isIdling) return;

        // 앞에 벽 있으면 방향 바꾸기
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, moveDir, wallCheckDistance, obstacleMask))
        {
            PickNewDirection(forceTurn: true);
        }

        transform.position += moveDir * speed * Time.deltaTime;

        // 바라보는 방향 맞춤
        if (moveDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 10f * Time.deltaTime);

        // 주기적으로 방향 바꾸기 / 멈추기
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (Random.value < idleChance)
            {
                StartCoroutine(IdleAndTurn());
            }
            else
            {
                PickNewDirection();
            }
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
        float angle = Random.Range(0f, 360f);
        Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

        // 턴할 때는 진행방향과 비슷한 방향 피하기
        if (forceTurn && Vector3.Dot(dir, moveDir) > 0.5f)
            dir = -dir;

        moveDir = dir;
        speed = Random.Range(speedMin, speedMax);
        timer = Random.Range(directionChangeIntervalMin, directionChangeIntervalMax);
    }
}
