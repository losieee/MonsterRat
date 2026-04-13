using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Fusion;

public interface IClearTarget
{
    float Remain01 { get; }
    float Weight { get; }
}

public class ClearManager : NetworkBehaviour
{
    public static ClearManager Instance;

    [System.Serializable]
    public class RandomAction
    {
        [Header("Objects")]
        public NetworkPrefabRef prefab;
        public Transform spawnPoint;

        [Header("Events")]
        public UnityEvent onInvoke;

        [Header("Percent")]
        public float weight = 1f;
    }

    [System.Serializable]
    public class PhaseRandom
    {
        public string name;
        [Range(0, 90)] public int minStep;
        [Range(0, 90)] public int maxStep;
        public List<RandomAction> actions = new List<RandomAction>();
    }

    [System.Serializable]
    public struct SpawnPlan
    {
        public int ratCount;
        public int roachCount;

        public SpawnPlan(int ratCount, int roachCount)
        {
            this.ratCount = ratCount;
            this.roachCount = roachCount;
        }

        public int TotalCount => ratCount + roachCount;
    }

    public Transform spawnRoot;
    public List<PhaseRandom> phases = new List<PhaseRandom>();

    [Header("플레이어 근처 생성 (바퀴벌레)")]
    [SerializeField] private NetworkPrefabRef roach;
    [SerializeField] private LayerMask floorMask;
    [SerializeField] private float spawnNearPlayerRadius = 2.5f;
    [SerializeField] private float spawnHeightOffset = 2f;
    [SerializeField] private float groundCheckDistance = 5f;
    [SerializeField] private float overlapCheckRadius = 0.35f;
    [SerializeField] private LayerMask spawnBlockMask;

    // 로컬 참조용 리스트(디버그/확장용)
    private readonly List<IClearTarget> targets = new List<IClearTarget>();

    // 네트워크 동기화되는 값들
    [Networked] public float BaselineTotal { get; set; }
    [Networked] public float RemainingTotal { get; set; }
    [Networked] public int LastStep { get; set; }
    [Networked] public NetworkBool HasInitializedTargets { get; set; }

    public float ClearRatio01
    {
        get
        {
            if (!HasInitializedTargets)
                return 0f;

            if (BaselineTotal <= 0f)
                return 0f;

            return Mathf.Clamp01(1f - (RemainingTotal / BaselineTotal));
        }
    }

    public int ClearPercent => Mathf.RoundToInt(ClearRatio01 * 100f);

    public override void Spawned()
    {
        Instance = this;

        if (spawnRoot == null)
            spawnRoot = transform;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // 청소해야 할것들 등록
    public void Register(IClearTarget target)
    {
        if (target == null) return;
        if (targets.Contains(target)) return;

        targets.Add(target);

        // 상태 권한만 네트워크 상태 변경
        if (!HasStateAuthority) return;

        float w = Mathf.Max(0f, target.Weight);

        BaselineTotal += w;
        RemainingTotal += Mathf.Clamp01(target.Remain01) * w;
        HasInitializedTargets = true;
    }

    // 해당 물체를 처리 했을 때 (등록 해제)
    public void Unregister(IClearTarget target)
    {
        if (target == null) return;

        bool removed = targets.Remove(target);
        if (!removed) return;

        if (!HasStateAuthority) return;

        float w = Mathf.Max(0f, target.Weight);

        // 현재 구조에선 "사라지는 것 = 청소 완료" 타입으로 보고 남은 양에서 차감
        RemainingTotal = Mathf.Max(0f, RemainingTotal - (Mathf.Clamp01(target.Remain01) * w));
    }

    // 부분 청소형 대상이 있을 때 수동 호출용
    public void NotifyTargetProgressChanged(IClearTarget target, float previousRemain01, float newRemain01)
    {
        if (target == null) return;
        if (!HasStateAuthority) return;

        float w = Mathf.Max(0f, target.Weight);

        float before = Mathf.Clamp01(previousRemain01) * w;
        float after = Mathf.Clamp01(newRemain01) * w;
        float delta = before - after;

        RemainingTotal = Mathf.Clamp(RemainingTotal - delta, 0f, BaselineTotal);
    }

    // 청소 상태(진행도) 초기화
    public void ResetProgress()
    {
        if (!HasStateAuthority) return;

        BaselineTotal = 0f;
        RemainingTotal = 0f;
        LastStep = 0;
        HasInitializedTargets = false;
    }

    private void Update()
    {
        // 단계 이벤트는 권한 쪽에서만 처리
        if (!HasStateAuthority) return;

        CheckStep(ClearRatio01);
    }

    // 10% 단위로 step으로 변환
    void CheckStep(float clearRatio01)
    {
        int step = Mathf.FloorToInt(clearRatio01 * 10f) * 10;
        step = Mathf.Clamp(step, 0, 100);

        if (step <= LastStep) return;

        for (int s = LastStep + 10; s <= step; s += 10)
        {
            if (s >= 10 && s <= 90)
                RunRandomFromPhase(s);
        }

        LastStep = step;
    }

    // 현재 step에 맞는 PhaseRandom를 찾아 그 중 하나 랜덤 실행
    void RunRandomFromPhase(int step)
    {
        PhaseRandom phase = FindPhase(step);
        if (phase == null) return; 
        if (phase.actions == null || phase.actions.Count == 0) return;

        RandomAction pick = PickWeightedRandom(phase.actions);
        if (pick == null) return;

        bool hasInvoke = pick.onInvoke != null && pick.onInvoke.GetPersistentEventCount() > 0;

        if (hasInvoke)
        {
            pick.onInvoke.Invoke();
            return;
        }

        if (pick.prefab.IsValid)
        {
            SpawnFromActionPoint(pick);
        }
    }

    // RandomAction에서 설정된 spawnPoint 위치에 프리팹을 스폰 <- spawnPoint, 프리팹 둘다 RandomAction 안에서 지정한거임
    void SpawnFromActionPoint(RandomAction action)
    {
        if (!HasStateAuthority) return;
        if (action == null) return;
        if (!action.prefab.IsValid) return;

        Transform point = action.spawnPoint != null ? action.spawnPoint : spawnRoot;
        if (point == null) point = transform;

        Runner.Spawn(action.prefab, point.position, point.rotation);
    }

    // 플레이어 근처 소환
    public void SpawnOneNearPlayer(NetworkPrefabRef prefab)
    {
        if (!HasStateAuthority) return;
        if (!prefab.IsValid) return;

        Transform player = FindAnyPlayerTransform();
        if (player == null) return;

        if (TryGetSpawnPositionNearPlayer(player, out Vector3 spawnPos))
        {
            Runner.Spawn(prefab, spawnPos, Quaternion.identity);
        }
    }

    // 플레이어 검색
    Transform FindAnyPlayerTransform()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null || players.Length == 0)
            return null;

        // 여러 명이면 한 명 랜덤 선택
        int index = Random.Range(0, players.Length);
        return players[index].transform;
    }

    // 플레이어 근처 바닥 찾기
    bool TryGetSpawnPositionNearPlayer(Transform player, out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;

        const int maxTry = 12;

        for (int i = 0; i < maxTry; i++)
        {
            Vector2 rand2D = Random.insideUnitCircle * spawnNearPlayerRadius;

            Vector3 origin = player.position + new Vector3(rand2D.x, spawnHeightOffset, rand2D.y);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckDistance, floorMask, QueryTriggerInteraction.Ignore))
                continue;

            Vector3 candidate = hit.point + Vector3.up * 0.1f;

            if (Physics.CheckSphere(candidate, overlapCheckRadius, spawnBlockMask, QueryTriggerInteraction.Ignore))
                continue;

            spawnPos = candidate;
            return true;
        }

        return false;
    }

    // 1스테이지 - 1Phase 전용
    public void SpawnStage1Phase1()
    {
        if (!HasStateAuthority) return;

        SpawnStage1Phase1Hazard();
    }

    public void SpawnStage1Phase1Hazard()
    {
        if (!HasStateAuthority) return;

        SpawnPlan plan = BuildStage1Phase1Plan();
        ExecuteStage1Phase1Plan(plan);
    }

    // 쥐 스폰에 쓰이는 Action 만 모음
    List<RandomAction> GetSpawnableActions(List<RandomAction> source)
    {
        List<RandomAction> result = new List<RandomAction>();

        if (source == null) return result;

        for (int i = 0; i < source.Count; i++)
        {
            RandomAction a = source[i];
            if (a == null) continue;
            if (!a.prefab.IsValid) continue;
            if (a.spawnPoint == null) continue;

            result.Add(a);
        }

        return result;
    }

    // BuildStage1Phase1Plan() 함수의 값을 받아 실제로 실행
    void ExecuteStage1Phase1Plan(SpawnPlan plan)
    {
        PhaseRandom phase = FindPhase(10);
        if (phase == null || phase.actions == null || phase.actions.Count == 0)
            return;

        // prefab, spawnPoint 없는 action은 제외
        List<RandomAction> ratActions = GetSpawnableActions(phase.actions);
        if (ratActions.Count == 0 && plan.ratCount > 0)
            return;

        // 쥐는 고정위치에서 스폰
        for (int i = 0; i < plan.ratCount; i++)
        {
            RandomAction ratPick = PickWeightedRandom(phase.actions);
            if (ratPick != null)
                SpawnFromActionPoint(ratPick);
        }
        // 바퀴는 플레이어 근처에서 소환
        for (int i = 0; i < plan.roachCount; i++)
        {
            SpawnOneNearPlayer(roach);
        }
    }

    // 문서에 있는 확률을 기반으로 스폰
    SpawnPlan BuildStage1Phase1Plan()
    {
        // Stage 1 - 쥐 40%, 바퀴벌레 40%, 혼합 20%
        float typeRoll = Random.Range(0f, 100f);

        // 총 마릿수 - 2마리 60%, 3마리 40%
        float countRoll = Random.Range(0f, 100f);
        int totalCount = (countRoll < 60f) ? 2 : 3;

        // 쥐 40%
        if (typeRoll < 40f)
        {
            return new SpawnPlan(totalCount, 0);
        }
        // 바퀴벌레 40%
        else if (typeRoll < 80f)
        {
            return new SpawnPlan(0, totalCount);
        }
        // 혼합 20%
        else
        {
            // stage 1~4에 혼합이면 무조건 쥐 1마리 고정
            int ratCount = 1;
            int roachCount = totalCount - ratCount;
            return new SpawnPlan(ratCount, roachCount);
        }
    }

    // 현재 step에 해당하는 PhaseRandom을 phases 리스트에서 탐색
    // PhaseRadom에서 랜덤으로 정한 값을 반환 - 뭐가 나올지 계산
    PhaseRandom FindPhase(int step)
    {
        for (int i = 0; i < phases.Count; i++)
        {
            var p = phases[i];
            if (p == null) continue;
            if (step < p.minStep || step > p.maxStep) continue;
            return p;
        }

        return null;
    }


    RandomAction PickWeightedRandom(List<RandomAction> list)
    {
        float total = 0f;

        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null) continue;

            bool hasInvoke = a.onInvoke != null && a.onInvoke.GetPersistentEventCount() > 0;
            bool hasSomething = a.prefab.IsValid || hasInvoke;
            if (!hasSomething) continue;

            total += Mathf.Max(0f, a.weight);
        }

        if (total <= 0f) return null;

        float r = Random.Range(0f, total);

        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null) continue;

            bool hasInvoke = a.onInvoke != null && a.onInvoke.GetPersistentEventCount() > 0;
            bool hasSomething = a.prefab.IsValid || hasInvoke;
            if (!hasSomething) continue;

            float w = Mathf.Max(0f, a.weight);
            r -= w;

            if (r < 0f)
                return a;
        }

        return null;
    }
}