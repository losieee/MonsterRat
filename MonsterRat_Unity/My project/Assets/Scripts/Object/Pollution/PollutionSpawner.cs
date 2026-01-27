using UnityEngine;

public class PollutionSpawner : MonoBehaviour
{
    [Header("Pollution")]
    public GameObject pollutionPrefab;
    public int spawnPollutionCount = 15;
    public BoxCollider spawnArea;
    public LayerMask pollutionMask;
    public LayerMask blockHitMask;
    public LayerMask overlapBlockMask;      // 겹침 방지
    public float rayDistance = 20f;
    public float checkRadius = 0.2f;

    [Header("Target")]
    public GameObject targetPrefab;
    public int spawnPlantCount = 3;
    public LayerMask plantMask = ~0;

    bool pollutionSpawnedOnce = false;
    bool plantSpawnedOnce = false;

    void Start() 
    { 
        SpawnRandomPollution(spawnPollutionCount); 
    }

    public void PollutionSpawnOnce()
    {
        if (pollutionSpawnedOnce) return;
        pollutionSpawnedOnce = true;
        SpawnRandomPollution(spawnPollutionCount);
    }

    public void TargetSpawnOnce()
    {
        if (plantSpawnedOnce) return;
        plantSpawnedOnce = true;
        SpawnRandomPlant(spawnPlantCount);
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

            // 오염 생성
            if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, pollutionMask, QueryTriggerInteraction.Ignore) ||
            Physics.Raycast(origin, -dir, out hit, rayDistance, pollutionMask, QueryTriggerInteraction.Ignore))
            {
                // 스폰 금지 레이어면 스킵
                if (((1 << hit.collider.gameObject.layer) & blockHitMask) != 0)
                    continue;

                Vector3 pos = hit.point;

                // 스폰 박스 범위 안인지 체크
                if (!spawnArea.bounds.Contains(pos))
                    continue;

                // 주변에 오브젝트가 있는지 (중복 방지)
                if (Physics.CheckSphere(pos + hit.normal * 0.02f, checkRadius, overlapBlockMask, QueryTriggerInteraction.Ignore))
                    continue;

                Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Instantiate(pollutionPrefab, pos, rot);
                spawned++;
            }
        }
    }

    void SpawnRandomPlant(int count)
    {
        if (targetPrefab == null || spawnArea == null) return;

        int spawned = 0;
        int tryCount = 0;
        int maxTry = count * 50;

        Vector3 center = spawnArea.transform.TransformPoint(spawnArea.center);
        float topY = center.y + (spawnArea.size.y * 0.5f);

        while (spawned < count && tryCount < maxTry)
        {
            tryCount++;

            // 랜덤 위치
            Vector3 randomPoint = GetRandomPointInBox(spawnArea);
            Vector3 origin = new Vector3(randomPoint.x, topY + 1f, randomPoint.z);
            Vector3 dir = Vector3.down;

            // 바닥만
            if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, plantMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 pos = hit.point;
                Quaternion rot = Quaternion.identity;
                Instantiate(targetPrefab, pos, rot);
                spawned++;
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
