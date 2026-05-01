using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Fusion;
using UnityEditor.Rendering.Universal;

public interface IClearTarget
{
    float Remain01 { get; }
    float Weight { get; }
}

public class ClearManager : NetworkBehaviour
{
    public enum SpawnDangerType
    {
        None,       // 그냥 일반 오브젝트 (wood, plant, gas, ...)
        Weak,       // 약 위험군 (쥐, 바퀴벌레)
        Monster     // 괴물 (BoxHead, Watcher, Legless)
    }

    private int weakMonsterSpawnCount = 0;      // 약위험군 출현 횟수
    private bool clearedAllGas = false;         // 클리어 하고 가스를 삭제한 적이 있는가

    public static ClearManager Instance;

    [System.Serializable]
    public class RandomAction
    {
        [Header("오브젝트 / 위치")]
        public NetworkPrefabRef prefab;
        public Transform spawnPoint;

        [Header("실행시킬 함수")]
        public UnityEvent onInvoke;

        [Header("확률")]
        public float weight = 1f;

        [Header("위험성 타입")]
        public SpawnDangerType dangerType = SpawnDangerType.None;

        [Header("쥐 스폰 개수 설정")]
        public bool useSpawnCountOptions = false;
        public List<RatSpawnCountOption> spawnCountOptions = new List<RatSpawnCountOption>();
    }

    [System.Serializable]
    public class RatSpawnCountOption
    {
        [Min(1)] public int count = 1;
        [Min(0f)] public float weight = 1f;
    }

    [System.Serializable]
    public class PhaseRandom
    {
        public string name;
        [Range(0, 90)] public int minStep;
        [Range(0, 90)] public int maxStep;
        public List<RandomAction> actions = new List<RandomAction>();
    }

    public Transform spawnRoot;
    public List<PhaseRandom> phases = new List<PhaseRandom>();
    [SerializeField] private List<RandomAction> ratSpawnActions = new List<RandomAction>();
    [SerializeField] private NetworkDoor clearDoorAnim;

    [Header("플레이어 근처 생성")]
    [SerializeField] private NetworkPrefabRef roach;
    [SerializeField] private NetworkPrefabRef boxHead;
    [SerializeField] private NetworkPrefabRef watcher;
    [SerializeField] private NetworkPrefabRef legless;
    [SerializeField] private LayerMask floorMask;
    [SerializeField] private float spawnNearPlayerRadius = 2.5f;        // 플레이어 근처 소환할 반경
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
        weakMonsterSpawnCount = 0;
        LastStep = 0;
        HasInitializedTargets = false;
        clearedAllGas = false;
    }

    private void Update()
    {
        // 단계 이벤트는 권한 쪽에서만 처리
        if (!HasStateAuthority) return;

        CheckStep(ClearRatio01);

        if (!clearedAllGas && ClearPercent >= 100)
        {
            clearedAllGas = true;

            if (PollutionSpawner.Instance != null)
                PollutionSpawner.Instance.DespawnAllGas();

            clearDoorAnim.TryOpenDoor();
        }
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
        int phaseTier = GetPhaseTier(step);

        if ((phaseTier == 2 || phaseTier == 3) && weakMonsterSpawnCount >= 2)
        {
            ForceSpawnStrongByPhase(phaseTier);
            return;
        }

        PhaseRandom phase = FindPhase(step);
        if (phase == null) return; 
        if (phase.actions == null || phase.actions.Count == 0) return;

        RandomAction pick = PickWeightedRandom(phase.actions);
        if (pick == null) return;

        bool hasInvoke = pick.onInvoke != null && pick.onInvoke.GetPersistentEventCount() > 0;

        if (hasInvoke)
        {
            pick.onInvoke.Invoke();
            ClassifyAndRecordAction(pick);
            return;
        }

        if (pick.prefab.IsValid)
        {
            SpawnFromActionPoint(pick);
            ClassifyAndRecordAction(pick);
        }
    }

    // Action에 있는 위험성 Type을 보고 기록해둠
    void ClassifyAndRecordAction(RandomAction action)
    {
        if (action == null) return;
        RecordSpawnResult(action.dangerType);
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

        if (TryGetSpawnPositionNearTarget(player, out Vector3 spawnPos))
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
    bool TryGetSpawnPositionNearTarget(Transform targetPlayer, out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;

        const int maxTry = 12;

        for (int i = 0; i < maxTry; i++)
        {
            Vector2 rand2D = Random.insideUnitCircle * spawnNearPlayerRadius;
            Vector3 origin = targetPlayer.position + new Vector3(rand2D.x, spawnHeightOffset, rand2D.y);

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

    /// <summary>
    /// 이벤트 용 스폰 함수 모음집
    /// </summary>
    // 쥐 만 스폰
    public void SpawnRatOnly()
    {
        if (!HasStateAuthority) return;

        List<RandomAction> spawnableRatActions = GetSpawnableActions(ratSpawnActions);
        if (spawnableRatActions.Count == 0) return;

        RandomAction baseAction = PickWeightedRandom(spawnableRatActions);
        if (baseAction == null) return;

        int count = PickSpawnCount(baseAction, 2);

        Debug.Log("Rat");

        for (int i = 0; i < count; i++)
        {
            RandomAction ratPick = PickWeightedRandom(spawnableRatActions);
            if (ratPick != null)
                SpawnFromActionPoint(ratPick);
        }
    }

    // 바퀴벌레 만 스폰
    public void SpawnRoachOnly()
    {
        if (!HasStateAuthority) return;

        List<RandomAction> spawnableRoachActions = GetSpawnableActions(ratSpawnActions);
        if (spawnableRoachActions.Count == 0) return;

        RandomAction baseAction = PickWeightedRandom(spawnableRoachActions);
        if (baseAction == null) return;

        int count = PickSpawnCount(baseAction, 1);

        Debug.Log("Roach");

        // PollutionSpawner에 있는 바퀴벌레 랜덤 스폰 함수를 불러옴
        // 왜냐 - 랜덤 범위가 저기 있으니까
        if (PollutionSpawner.Instance != null)
        {
            PollutionSpawner.Instance.SpawnRoachesInRandomAreas(roach, count);
        }
    }

    // 혼합 스폰 (쥐는 무조건 1마리 스폰)
    public void SpawnMixed()
    {
        if (!HasStateAuthority) return;
        List<RandomAction> spawnableRoachActions = GetSpawnableActions(ratSpawnActions);
        if (spawnableRoachActions.Count == 0) return;

        RandomAction baseAction = PickWeightedRandom(spawnableRoachActions);
        if (baseAction == null) return;

        int ratCount = 1;
        int roachCount = PickSpawnCount(baseAction, 1);

        Debug.Log("Mix");
        SpawnFixedRats(ratCount);
        if (PollutionSpawner.Instance != null)
            PollutionSpawner.Instance.SpawnRoachesInRandomAreas(roach, roachCount);
    }

    public void SpawnBoxHead()
    {
        if (!HasStateAuthority) return;
        Debug.Log("BoxHead");
        SpawnOneNearPlayer(boxHead);
    }

    public void SpawnWatcher()
    {
        if (!HasStateAuthority) return;
        Debug.Log("Watcher");
        SpawnOneNearPlayer(watcher);
    }

    public void SpawnLegless()
    {
        if (!HasStateAuthority) return;
        Debug.Log("Legless");
        SpawnOneNearPlayer(legless);
    }

    /// <summary>
    /// 여기까지 모음집
    /// </summary>
    
    // 현재 진행도가 무슨 step인지
    int GetPhaseTier(int step)
    {
        if (step >= 10 && step < 30) return 1;  // 10~30%
        if (step >= 30 && step < 60) return 2;  // 30~60%
        if (step >= 60 && step < 90) return 3;  // 60~90%
        return 0;
    }

    // 약 위험군 나오는걸 저장해두는 함수
    void RecordSpawnResult(SpawnDangerType type)
    {
        if (type == SpawnDangerType.Weak)
            weakMonsterSpawnCount++;
        else if (type == SpawnDangerType.Monster)
            weakMonsterSpawnCount = 0;
    }

    // 약 위험군이 2번 나왔을 때 Phase2(30~60%)일때는 박스헤드 스폰, Phase3(60~90%)일때는 괴물들 중 한마리 랜덤 스폰
    void ForceSpawnStrongByPhase(int phaseTier)
    {
        if (phaseTier == 2)
        {
            SpawnBoxHead();
            RecordSpawnResult(SpawnDangerType.Monster);
        }
        else if (phaseTier == 3)
        {
            int roll = Random.Range(0, 3);

            switch (roll)
            {
                case 0:
                    SpawnBoxHead();
                    break;
                case 1:
                    SpawnWatcher();
                    break;
                default:
                    SpawnLegless();
                    break;
            }

            RecordSpawnResult(SpawnDangerType.Monster);
        }
    }

    // 쥐 마릿수 계산
    int PickSpawnCount(RandomAction action, int defaultCount = 1)
    {
        if (action == null)
            return defaultCount;

        if (!action.useSpawnCountOptions || action.spawnCountOptions == null || action.spawnCountOptions.Count == 0)
            return defaultCount;

        float total = 0f;

        for (int i = 0; i < action.spawnCountOptions.Count; i++)
        {
            var option = action.spawnCountOptions[i];
            if (option == null) continue;

            total += Mathf.Max(0f, option.weight);
        }

        if (total <= 0f)
            return defaultCount;

        float r = Random.Range(0f, total);

        for (int i = 0; i < action.spawnCountOptions.Count; i++)
        {
            var option = action.spawnCountOptions[i];
            if (option == null) continue;

            float w = Mathf.Max(0f, option.weight);
            r -= w;

            if (r < 0f)
                return Mathf.Max(1, option.count);
        }

        return defaultCount;
    }

    // 정해진 위치 중 쥐 랜덤 소환
    void SpawnFixedRats(int count)
    {
        if (count <= 0) return;

        List<RandomAction> spawnableRatActions = GetSpawnableActions(ratSpawnActions);
        if (spawnableRatActions.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            RandomAction ratPick = PickWeightedRandom(spawnableRatActions);
            if (ratPick != null)
                SpawnFromActionPoint(ratPick);
        }
    }

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