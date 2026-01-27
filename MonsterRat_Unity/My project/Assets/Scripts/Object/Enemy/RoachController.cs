using System.Collections;
using System.Collections.Generic;
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
    public LayerMask obstacleMask;

    [Header("Pollution Trail")]
    public GameObject pollutionPrefab;
    public float spawnEveryDistance = 0.5f;     // 이 거리만큼 이동할 때마다 생성
    public float spawnYOffset = 0.0f;           // 높이 조절
    public float minDistanceFromLast = 0.05f;   // 너무 가깝게 생성되는거 방지

    Vector3 moveDir;
    float speed;
    float timer;
    Vector3 lastSpawnPos;

    void Start()
    {
        PickNewDirection();
        lastSpawnPos = transform.position;
        SpawnPollution(transform.position);
    }

    void Update()
    {
        // 앞에 벽 있으면 방향 변경
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, moveDir, wallCheckDistance, obstacleMask))
        {
            PickNewDirection(forceTurn: true);
        }

        // 이동
        transform.position += moveDir * speed * Time.deltaTime;

        // 바라보는 방향 맞춤
        if (moveDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 10f * Time.deltaTime);

        // 지나간 경로에 오염물질 생성
        TrySpawnTrail();

        // 주기적으로 방향 바꾸기
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            PickNewDirection();
        }
    }

    void TrySpawnTrail()
    {
        if (pollutionPrefab == null) return;

        float dist = Vector3.Distance(transform.position, lastSpawnPos);
        if (dist < spawnEveryDistance) return;

        Vector3 from = lastSpawnPos;
        Vector3 to = transform.position;
        Vector3 dir = (to - from).normalized;

        float remaining = dist;
        while (remaining >= spawnEveryDistance)
        {
            from += dir * spawnEveryDistance;

            // 중복 방지
            if (Vector3.Distance(from, lastSpawnPos) >= minDistanceFromLast)
            {
                SpawnPollution(from);
                lastSpawnPos = from;
            }

            remaining = Vector3.Distance(to, lastSpawnPos);
        }
    }

    void SpawnPollution(Vector3 pos)
    {
        Vector3 p = pos + Vector3.up * spawnYOffset;
        Instantiate(pollutionPrefab, p, Quaternion.identity);
    }

    void PickNewDirection(bool forceTurn = false)
    {
        float angle = Random.Range(0f, 360f);
        Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

        if (forceTurn && Vector3.Dot(dir, moveDir) > 0.5f)
            dir = -dir;

        moveDir = dir;
        speed = Random.Range(speedMin, speedMax);
        timer = Random.Range(directionChangeIntervalMin, directionChangeIntervalMax);
    }
}
