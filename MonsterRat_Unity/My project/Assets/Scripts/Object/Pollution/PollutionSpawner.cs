using UnityEngine;

public class PollutionSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject pollutionPrefab;
    public int spawnCount = 15;
    public BoxCollider spawnArea;
    public LayerMask surfaceMask = ~0;
    public float rayDistance = 20f;

    void Start()
    {
        SpawnRandomPollution(spawnCount);
    }

    void SpawnRandomPollution(int count)
    {
        if (pollutionPrefab == null || spawnArea == null) return;

        int spawned = 0;
        int tryCount = 0;
        // 무한루프 방지
        int maxTry = count * 30;

        while (spawned < count && tryCount < maxTry)
        {
            tryCount++;

            // 랜덤 위치
            Vector3 origin = GetRandomPointInBox(spawnArea);

            // 임의 방향으로 raycast
            Vector3 dir = Random.onUnitSphere.normalized;

            // 맞으면 오염 생성
            if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, surfaceMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 pos = hit.point;

                // 맞은 표면의 방향에 맞춰 회전
                Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);

                Instantiate(pollutionPrefab, pos, rot);
                spawned++;
            }
            else
            {
                // 실패하면 반대 방향으로 한 번 더 쏘기
                if (Physics.Raycast(origin, -dir, out RaycastHit hit2, rayDistance, surfaceMask, QueryTriggerInteraction.Ignore))
                {
                    Vector3 pos = hit2.point;
                    Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit2.normal);

                    Instantiate(pollutionPrefab, pos, rot);
                    spawned++;
                }
            }
        }

    }

    Vector3 GetRandomPointInBox(BoxCollider box)
    {
        Vector3 center = box.transform.TransformPoint(box.center);
        Vector3 size = box.size;

        float x = Random.Range(-size.x * 0.5f, size.x * 0.5f);
        float y = Random.Range(-size.y * 0.5f, size.y * 0.5f);
        float z = Random.Range(-size.z * 0.5f, size.z * 0.5f);

        Vector3 localPos = new Vector3(x, y, z);
        return center + box.transform.TransformDirection(localPos);
    }
}
