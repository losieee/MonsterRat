using UnityEngine;
using Fusion;

public class PollutionSpawner : NetworkBehaviour
{
    [Header("Pollution")]
    public NetworkPrefabRef pollutionPrefab;
    public int spawnPollutionCount = 15;
    public BoxCollider spawnArea;
    public LayerMask pollutionMask;
    public LayerMask blockHitMask;
    public LayerMask overlapBlockMask;
    public float rayDistance = 20f;
    public float checkRadius = 0.2f;

    [Header("Target")]
    public NetworkPrefabRef plantPrefab;
    public NetworkPrefabRef monsterPrefab;
    public NetworkPrefabRef woodPrefab;
    public int spawnPlantCount = 3;
    public LayerMask plantMask = ~0;

    private bool pollutionSpawnedOnce = false;

    public override void Spawned()
    {
        if (!HasStateAuthority) return;

        PollutionSpawnOnce();
    }

    public void PollutionSpawnOnce()
    {
        if (!HasStateAuthority) return;
        if (pollutionSpawnedOnce) return;

        pollutionSpawnedOnce = true;
        SpawnRandomPollution(spawnPollutionCount);
    }

    public void WoodSpawnOnce()
    {
        if (!HasStateAuthority) return;
        SpawnRandomWood();
    }

    public void TargetSpawnOnce()
    {
        if (!HasStateAuthority) return;
        SpawnRandomPlant(spawnPlantCount);
    }

    public void MonsterSpawnOnce()
    {
        if (!HasStateAuthority) return;
        SpawnMonster();
    }

    void SpawnRandomPollution(int count)
    {
        if (spawnArea == null) return;

        int spawned = 0;
        int tryCount = 0;
        int maxTry = count * 30;

        while (spawned < count && tryCount < maxTry)
        {
            tryCount++;

            Vector3 origin = GetRandomPointInBox(spawnArea);
            Vector3 dir = Random.onUnitSphere.normalized;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, pollutionMask, QueryTriggerInteraction.Ignore) ||
                Physics.Raycast(origin, -dir, out hit, rayDistance, pollutionMask, QueryTriggerInteraction.Ignore))
            {
                if (((1 << hit.collider.gameObject.layer) & blockHitMask) != 0)
                    continue;

                Vector3 pos = hit.point + hit.normal * 0.03f;

                if (!spawnArea.bounds.Contains(pos))
                    continue;

                if (Physics.CheckSphere(pos, checkRadius, overlapBlockMask, QueryTriggerInteraction.Ignore))
                    continue;

                Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);

                Runner.Spawn(pollutionPrefab, pos, rot);
                spawned++;
            }
        }
    }

    void SpawnRandomWood()
    {
        Vector3 center = spawnArea.transform.TransformPoint(spawnArea.center);
        float topY = center.y + (spawnArea.size.y * 0.5f);

        Vector3 randomPoint = GetRandomPointInBox(spawnArea);
        Vector3 origin = new Vector3(randomPoint.x, topY + 1f, randomPoint.z);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, plantMask, QueryTriggerInteraction.Ignore))
        {
            Runner.Spawn(woodPrefab, hit.point, Quaternion.identity);
        }
    }

    void SpawnRandomPlant(int count)
    {
        if (spawnArea == null) return;

        int spawned = 0;
        int tryCount = 0;
        int maxTry = count * 50;

        Vector3 center = spawnArea.transform.TransformPoint(spawnArea.center);
        float topY = center.y + (spawnArea.size.y * 0.5f);

        while (spawned < count && tryCount < maxTry)
        {
            tryCount++;

            Vector3 randomPoint = GetRandomPointInBox(spawnArea);
            Vector3 origin = new Vector3(randomPoint.x, topY + 1f, randomPoint.z);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, plantMask, QueryTriggerInteraction.Ignore))
            {
                Runner.Spawn(plantPrefab, hit.point, Quaternion.identity);
                spawned++;
            }
        }
    }

    void SpawnMonster()
    {
        Vector3 center = spawnArea.transform.TransformPoint(spawnArea.center);
        float topY = center.y + (spawnArea.size.y * 0.5f);

        Vector3 randomPoint = GetRandomPointInBox(spawnArea);
        Vector3 origin = new Vector3(randomPoint.x, topY + 1f, randomPoint.z);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, plantMask, QueryTriggerInteraction.Ignore))
        {
            Runner.Spawn(monsterPrefab, hit.point, Quaternion.identity);
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