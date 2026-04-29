using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class PollutionSpawner : NetworkBehaviour
{
    public static PollutionSpawner Instance;

    [Header("얼룩")]
    [SerializeField] private NetworkPrefabRef pollutionPrefab;
    [SerializeField] private float pollutionThickness = 0.1f;       // 얼룩 두께
    [SerializeField] private float surfaceGap = 0.0025f;
    [SerializeField] private Vector3 pollutionHalfExtents = new Vector3(0.75f, 0.05f, 0.75f);       // 얼룩 크기 검사
    [SerializeField] private Vector2 randomScaleRange = new Vector2(0.8f, 2f);          // 얼룩 랜덤 크기 범위
    [SerializeField] private Vector3 basePollutionScale = new Vector3(1.5f, 0.1f, 1.5f);            // 기본 얼룩 크기
    [SerializeField] private int spawnPollutionCount = 15;
    [SerializeField] private float minPollutionSpacing = 1.2f;      // 얼룩끼리의 간격
    [Header("Layer Mask")]
    [SerializeField] private LayerMask pollutionMask;
    [SerializeField] private LayerMask blockHitMask;
    [SerializeField] private LayerMask overlapBlockMask;

    [Header("쓰레기")]
    [SerializeField] private NetworkPrefabRef[] trashPrefabs;
    [SerializeField] private int spawnTrashCount = 5;
    [SerializeField] private LayerMask floorMask;
    [SerializeField] private LayerMask spawnBlockMask;
    [SerializeField] private float trashSpawnHeightOffset = 1f;

    [Header("가스")]
    [SerializeField] private NetworkPrefabRef rangeGas;

    [Header("소환 범위")]
    [SerializeField] private GameObject cleaningTargets;
    [SerializeField] private BoxCollider[] spawnArea;

    [SerializeField] private float rayDistance = 20f;
    [SerializeField] private float checkRadius = 0.2f;

    [Header("오염 식물")]
    [SerializeField] private NetworkPrefabRef plantPrefab;
    [SerializeField] private int spawnPlantCount = 3;

    [Header("Json")]
    [SerializeField] private TextAsset stageDBJson;
    [SerializeField] private int currentStageNum = 1;

    private List<StageJson> stageList = new List<StageJson>();

    private bool pollutionSpawnedOnce = false;

    // 100% 되면 삭제시킬것들
    private readonly List<NetworkObject> spawnedGases = new List<NetworkObject>();
    private readonly List<NetworkObject> spawnedPlants = new List<NetworkObject>();
    private readonly List<NetworkObject> spawnedPollutions = new List<NetworkObject>();
    private bool gasCleared = false;

    public override void Spawned()
    {
        Instance = this;
        
        if (!HasStateAuthority) return;

        LoadStageData();
        ApplyStageData(currentStageNum);

        PollutionSpawnOnce();
        TrashSpawnOnce();
    }

    // Json 불러오기
    void LoadStageData()
    {
        if (stageDBJson == null)
            return;

        string wrappedJson = "{\"stages\":" + stageDBJson.text + "}";

        StageDataList dataList = JsonUtility.FromJson<StageDataList>(wrappedJson);

        if (dataList == null || dataList.stages == null)
            return;

        stageList = dataList.stages;
    }

    void ApplyStageData(int stageNum)
    {
        if (stageList == null || stageList.Count == 0)
            return;

        StageJson data = stageList.Find(stage => stage.Stg_Num == stageNum);

        if (data == null)
            return;

        spawnPollutionCount = data.Stg_Stain;
        spawnTrashCount = data.Stg_Trash;
        spawnPlantCount = data.Stg_Pipe;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void DespawnAllCleaningObjects()
    {
        if (!HasStateAuthority) return;

        // 모든 나무 삭제
        for (int i = spawnedPlants.Count - 1; i >= 0; i--)
        {
            if (spawnedPlants[i] != null)
                Runner.Despawn(spawnedPlants[i]);
        }
        spawnedPlants.Clear();

        // 모든 얼룩 삭제
        for (int i = spawnedPollutions.Count - 1; i >= 0; i--)
        {
            if (spawnedPollutions[i] != null)
                Runner.Despawn(spawnedPollutions[i]);
        }
        spawnedPollutions.Clear();

        // 모든 가스 삭제
        for (int i = spawnedGases.Count - 1; i >= 0; i--)
        {
            NetworkObject gas = spawnedGases[i];

            if (gas != null)
            {
                GasRandomSpawner gasSpawner = gas.GetComponent<GasRandomSpawner>();
                if (gasSpawner != null)
                    gasSpawner.DespawnAllSmallGases();

                Runner.Despawn(gas);
            }
        }
        spawnedGases.Clear();

        gasCleared = false;
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

        Debug.Log("Wood");

        for (int i = 0; i < spawnPlantCount; i++)
        {
            SpawnRandomPlant();
        }
    }

    public void TrashSpawnOnce()
    {
        if (!HasStateAuthority) return;
        SpawnRandomTrash(spawnTrashCount);
    }

    public void MonsterSpawnOnce()
    {
        if (!HasStateAuthority) return;
        SpawnMonster();
    }

    // 랜덤 얼룩 생성
    void SpawnRandomPollution(int count)
    {
        if (!HasValidSpawnAreas()) return;

        int spawned = 0;
        int tryCount = 0;
        int maxTry = count * 30;

        List<Vector3> placedPollutionPositions = new List<Vector3>();

        while (spawned < count && tryCount < maxTry)
        {
            tryCount++;

            BoxCollider selectedArea = GetRandomSpawnArea();
            if (selectedArea == null) continue;

            Vector3 origin = GetRandomPointInBox(selectedArea, 0.8f, 0.1f, 0.8f);
            Vector3 dir = Random.onUnitSphere.normalized;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, pollutionMask, QueryTriggerInteraction.Ignore) ||
                Physics.Raycast(origin, -dir, out hit, rayDistance, pollutionMask, QueryTriggerInteraction.Ignore))
            {
                if (((1 << hit.collider.gameObject.layer) & blockHitMask) != 0)
                    continue;

                Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Vector3 pos = hit.point + hit.normal * ((pollutionThickness * 0.5f) + surfaceGap);

                if (!selectedArea.bounds.Contains(pos))
                    continue;

                float scaleMul = Random.Range(randomScaleRange.x, randomScaleRange.y);
                bool placed = false;

                for (int i = 0; i < 3; i++)
                {
                    Vector3 spawnScale = new Vector3(
                        basePollutionScale.x * scaleMul,
                        basePollutionScale.y,
                        basePollutionScale.z * scaleMul
                    );

                    float halfWidth = spawnScale.x * 0.5f;
                    float halfHeight = spawnScale.z * 0.5f;

                    // 벽 면적 안에 들어가는지 검사
                    if (!CanPlacePollutionOnSurface(hit, halfWidth, halfHeight, 0.2f, pollutionMask))
                    {
                        scaleMul *= 0.7f;
                        continue;
                    }

                    // 실제 랜덤 크기로 겹침 검사
                    Vector3 checkHalfExtents = new Vector3(
                        spawnScale.x * 0.5f,
                        spawnScale.y * 0.5f,
                        spawnScale.z * 0.5f
                    );

                    if (Physics.CheckBox(pos, checkHalfExtents, rot, overlapBlockMask, QueryTriggerInteraction.Ignore))
                    {
                        scaleMul *= 0.7f;
                        continue;
                    }

                    // 다른 얼룩과의 거리 검사
                    float spacing = minPollutionSpacing + (Mathf.Max(spawnScale.x, spawnScale.z) * 0.25f);
                    if (!IsFarEnoughFromOtherPollution(pos, placedPollutionPositions, spacing))
                    {
                        scaleMul *= 0.85f;
                        continue;
                    }

                    NetworkObject obj = Runner.Spawn(pollutionPrefab, pos, rot);

                    if (obj != null)
                    {
                        obj.transform.localScale = spawnScale;
                        spawnedPollutions.Add(obj);
                        placed = true;
                    }

                    break;
                }

                if (placed)
                    spawned++;
            }
        }
    }

    // 얼룩이 새로 생성되면 리스트에 등록
    public void RegisterSpawnedPollution(NetworkObject obj)
    {
        if (obj == null) return;

        if (!spawnedPollutions.Contains(obj))
            spawnedPollutions.Add(obj);
    }
    // 등록 해제
    public void UnregisterSpawnedPollution(NetworkObject obj)
    {
        if (obj == null) return;

        spawnedPollutions.Remove(obj);
    }

    // 길막용 나무
    void SpawnRandomPlant()
    {
        if (!HasValidSpawnAreas()) return;
        if (!plantPrefab.IsValid) return;

        int tryCount = 0;
        int maxTry = 30;

        while (tryCount < maxTry)
        {
            tryCount++;

            BoxCollider selectedArea = GetRandomSpawnArea();
            if (selectedArea == null) continue;

            Vector3 center = selectedArea.transform.TransformPoint(selectedArea.center);
            float topY = center.y + (selectedArea.size.y * 0.5f);

            Vector3 randomPoint = GetRandomPointInBox(selectedArea, 0.3f, 0.1f, 0.3f);
            Vector3 origin = new Vector3(randomPoint.x, topY + trashSpawnHeightOffset, randomPoint.z);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, floorMask, QueryTriggerInteraction.Ignore))
                continue;

            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Floor"))
                continue;

            Vector3 spawnPos = hit.point + Vector3.up * 0.15f;

            if (Physics.CheckSphere(spawnPos, 0.2f, spawnBlockMask, QueryTriggerInteraction.Ignore))
                continue;

            NetworkObject obj = Runner.Spawn(plantPrefab, spawnPos, Quaternion.identity);

            if (obj != null)
                spawnedPlants.Add(obj);
            return;
        }
    }

    // 가스 생성
    public void SpawnGas()
    {
        if (!HasStateAuthority) return;
        if (!rangeGas.IsValid) return;

        BoxCollider selectedArea = GetRandomSpawnArea();
        if (selectedArea == null) return;

        Vector3 center = selectedArea.transform.TransformPoint(selectedArea.center);

        Debug.Log("Gas");
        NetworkObject gasObj = Runner.Spawn(rangeGas, center, Quaternion.identity);

        if (gasObj != null)
            spawnedGases.Add(gasObj);
    }

    // 바퀴벌레 랜덤 스폰
    public void SpawnRoachesInRandomAreas(NetworkPrefabRef roachPrefab, int count)
    {
        if (!HasStateAuthority) return;
        if (!roachPrefab.IsValid) return;
        if (!HasValidSpawnAreas()) return;
        if (count <= 0) return;

        List<BoxCollider> usableAreas = new List<BoxCollider>();
        for (int i = 0; i < spawnArea.Length; i++)
        {
            if (spawnArea[i] != null)
                usableAreas.Add(spawnArea[i]);
        }

        if (usableAreas.Count == 0) return;

        // count가 2~3이라면 1~count개의 범위를 랜덤 선택
        int areaPickCount = Mathf.Min(Random.Range(1, count + 1), usableAreas.Count);

        // 범위 섞기
        for (int i = 0; i < usableAreas.Count; i++)
        {
            int swap = Random.Range(i, usableAreas.Count);
            (usableAreas[i], usableAreas[swap]) = (usableAreas[swap], usableAreas[i]);
        }

        List<BoxCollider> selectedAreas = usableAreas.GetRange(0, areaPickCount);

        int spawned = 0;
        int tryCount = 0;
        int maxTry = count * 20;

        while (spawned < count && tryCount < maxTry)
        {
            tryCount++;

            BoxCollider selectedArea = selectedAreas[Random.Range(0, selectedAreas.Count)];
            if (selectedArea == null) continue;

            Vector3 center = selectedArea.transform.TransformPoint(selectedArea.center);
            float topY = center.y + (selectedArea.size.y * 0.5f);

            Vector3 randomPoint = GetRandomPointInBox(selectedArea, 0.3f, 0.1f, 0.3f);
            Vector3 origin = new Vector3(randomPoint.x, topY + trashSpawnHeightOffset, randomPoint.z);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, floorMask, QueryTriggerInteraction.Ignore))
                continue;

            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Floor"))
                continue;

            Vector3 spawnPos = hit.point + Vector3.up * 0.1f;

            if (Physics.CheckSphere(spawnPos, 0.2f, spawnBlockMask, QueryTriggerInteraction.Ignore))
                continue;

            Runner.Spawn(roachPrefab, spawnPos, Quaternion.identity);
            spawned++;
        }
    }

    // 가스 전부 제거 (클리어 됐을 때 맵 깔끔하게)
    public void DespawnAllGas()
    {
        if (!HasStateAuthority) return;
        if (gasCleared) return;

        gasCleared = true;

        for (int i = spawnedGases.Count - 1; i >= 0; i--)
        {
            NetworkObject gas = spawnedGases[i];

            if (gas != null)
                Runner.Despawn(gas);
        }

        spawnedGases.Clear();
    }

    // 쓰레기 생성
    void SpawnRandomTrash(int count)
    {
        if (!HasValidSpawnAreas()) return;
        if (trashPrefabs == null || trashPrefabs.Length == 0) return;

        int spawned = 0;
        int tryCount = 0;
        int maxTry = count * 30;

        while (spawned < count && tryCount < maxTry)
        {
            tryCount++;

            BoxCollider selectedArea = GetRandomSpawnArea();
            if (selectedArea == null) continue;

            Vector3 center = selectedArea.transform.TransformPoint(selectedArea.center);
            float topY = center.y + (selectedArea.size.y * 0.5f);

            Vector3 randomPoint = GetRandomPointInBox(selectedArea, 0.3f, 0.1f, 0.3f);
            Vector3 origin = new Vector3(randomPoint.x, topY + trashSpawnHeightOffset, randomPoint.z);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, floorMask, QueryTriggerInteraction.Ignore))
                continue;

            // floor 레이어인지 한 번 더 확실히 체크
            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Floor"))
                continue;

            int randomIndex = Random.Range(0, trashPrefabs.Length);
            NetworkPrefabRef selectedTrashPrefab = trashPrefabs[randomIndex];

            Vector3 spawnPos = hit.point + Vector3.up * 0.15f;
            
            if (Physics.CheckSphere(spawnPos, 0.2f, spawnBlockMask, QueryTriggerInteraction.Ignore))
                continue;

            // 나중에 치트로 다 한번에 다 없애야할 때 밑에꺼 주석 후 주석 해제
            //NetworkObject obj = Runner.Spawn(selectedTrashPrefab, spawnPos, Quaternion.identity);
            //if (obj != null)
            //{
            //    obj.transform.SetParent(cleaningTargets.transform, true);
            //}

            Runner.Spawn(selectedTrashPrefab, spawnPos, Quaternion.identity);
            spawned++;
        }
    }

    // 몬스터 생성
    void SpawnMonster()
    {
        BoxCollider selectedArea = GetRandomSpawnArea();
        if (selectedArea == null) return;

        Vector3 center = selectedArea.transform.TransformPoint(selectedArea.center);
        float topY = center.y + (selectedArea.size.y * 0.5f);

        Vector3 randomPoint = GetRandomPointInBox(selectedArea, 0.8f, 0.1f, 0.8f);
        Vector3 origin = new Vector3(randomPoint.x, topY + 1f, randomPoint.z);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, floorMask, QueryTriggerInteraction.Ignore))
        {
            Runner.Spawn(plantPrefab, hit.point, Quaternion.identity);
        }
    }

    // 얼룩 생성하기 전 벽의 위,아래,양옆 공간 넉넉한지 소환해도 되는지 확인
    bool CanPlacePollutionOnSurface(
    RaycastHit centerHit,
    float halfWidth,
    float halfHeight,
    float probeOffset,
    LayerMask surfaceMask)
    {
        Vector3 normal = centerHit.normal;

        // 벽면 기준 가로축 만들기
        Vector3 right = Vector3.Cross(Vector3.up, normal);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(Vector3.forward, normal);
        right.Normalize();

        // 벽면 기준 세로축 만들기
        Vector3 up = Vector3.Cross(normal, right).normalized;

        Vector3[] testOffsets =
        {
        right * halfWidth,
        -right * halfWidth,
        up * halfHeight,
        -up * halfHeight
    };

        foreach (Vector3 offset in testOffsets)
        {
            // 벽 바깥쪽에서 안쪽으로 다시 쏴서
            // 그 위치에도 같은 벽이 있는지 확인
            Vector3 rayStart = centerHit.point + normal * probeOffset + offset;

            if (!Physics.Raycast(rayStart, -normal, out RaycastHit sideHit, probeOffset * 2f, surfaceMask, QueryTriggerInteraction.Ignore))
                return false;

            // 노멀 방향이 너무 다르면 모서리/다른 면이므로 실패
            if (Vector3.Dot(centerHit.normal, sideHit.normal) < 0.95f)
                return false;
        }

        return true;
    }

    // 스폰 될 얼룩 근처에 이미 얼룩이 있는지 검사
    bool IsFarEnoughFromOtherPollution(Vector3 candidatePos, List<Vector3> placedPositions, float minDistance)
    {
        float minDistanceSqr = minDistance * minDistance;

        for (int i = 0; i < placedPositions.Count; i++)
        {
            if ((placedPositions[i] - candidatePos).sqrMagnitude < minDistanceSqr)
                return false;
        }

        return true;
    }

    bool HasValidSpawnAreas()
    {
        if (spawnArea == null || spawnArea.Length == 0)
            return false;

        for (int i = 0; i < spawnArea.Length; i++)
        {
            if (spawnArea[i] != null)
                return true;
        }

        return false;
    }

    BoxCollider GetRandomSpawnArea()
    {
        if (spawnArea == null || spawnArea.Length == 0)
            return null;

        int usableCount = spawnArea.Length;

        int safety = 20;
        while (safety-- > 0)
        {
            int index = Random.Range(0, usableCount);
            if (spawnArea[index] != null)
                return spawnArea[index];
        }

        return null;
    }

    Vector3 GetRandomPointInBox(BoxCollider box, float marginX, float marginY, float marginZ)
    {
        Vector3 center = box.transform.TransformPoint(box.center);
        Vector3 size = box.size;

        float halfX = Mathf.Max(0f, size.x * 0.5f - marginX);
        float halfY = Mathf.Max(0f, size.y * 0.5f - marginY);
        float halfZ = Mathf.Max(0f, size.z * 0.5f - marginZ);

        float x = Random.Range(-halfX, halfX);
        float y = Random.Range(-halfY, halfY);
        float z = Random.Range(-halfZ, halfZ);

        Vector3 localPos = new Vector3(x, y, z);
        return center + box.transform.TransformDirection(localPos);
    }
}