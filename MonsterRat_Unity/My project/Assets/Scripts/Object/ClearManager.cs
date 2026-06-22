using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Fusion;
using System.Collections;

public interface IClearTarget
{
    float Remain01 { get; }
    float Weight { get; }
}

public enum RePollutionDebuffType       // 잔향 디버프
{
    None,
    FastStep,           // 위험요소 등장 간격 축소
    DoubleEvent,        // 위험요소 중첩
    ChaosPhase          // 위험요소 등장 순서 교란
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

    [Header("현재 스테이지")]
    public int stageNum = 1;
    public bool cheatScene;

    // 로컬 참조용 리스트(디버그/확장용)
    private readonly List<IClearTarget> targets = new List<IClearTarget>();
    // 현재 적용중인 디버프 저장용
    private RePollutionDebuffType currentDebuff = RePollutionDebuffType.None;

    private bool debuffInitialized = false;
    private bool ignoreFirstStepAfterSetup = true;

    // 스테이지 시작 전 준비가 되었는가 (스테이지 초기화가 완료 되었는가)
    private bool isStageReady = false;
    private bool isInitializingTargets = false;

    // 네트워크 동기화되는 값들
    [Networked] public float BaselineTotal { get; set; }
    [Networked] public float RemainingTotal { get; set; }
    [Networked] public int LastStep { get; set; }
    [Networked] public NetworkBool HasInitializedTargets { get; set; }

    // 발표용 치트
    private readonly List<NetworkObject> spawnedDangerObjects = new List<NetworkObject>();
    private bool isStageCompleted = false;

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

        if (HasStateAuthority)
        {
            SetupRePollutionDebuff();
        }

        ResetProgress();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // 디버프 랜덤 결정
    void SetupRePollutionDebuff()
    {
        currentDebuff = RePollutionDebuffType.None;

        if (stageNum < 2)
            return;

        if (RePollutionSpawner.Instance == null)
            return;

        if (!RePollutionSpawner.Instance.HasRemainingRePollution)
            return;

        int random = Random.Range(0, 3);

        switch (random)
        {
            case 0:
                currentDebuff = RePollutionDebuffType.FastStep;
                break;

            case 1:
                currentDebuff = RePollutionDebuffType.DoubleEvent;
                break;

            case 2:
                currentDebuff = RePollutionDebuffType.ChaosPhase;
                break;
        }

        Debug.Log(currentDebuff);
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
        RemainingTotal += w;
    }

    // 해당 물체를 처리 했을 때 (등록 해제)
    public void Unregister(IClearTarget target)
    {
        if (target == null) return;

        bool removed = targets.Remove(target);
        if (!removed) return;

        if (!HasStateAuthority) return;

        if (isInitializingTargets)
            return;

        float w = Mathf.Max(0f, target.Weight);

        // 현재 구조에선 "사라지는 것 = 청소 완료" 타입으로 보고 남은 양에서 차감
        RemainingTotal = Mathf.Max(0f, RemainingTotal - w);
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

        isInitializingTargets = true;

        targets.Clear();
        spawnedDangerObjects.Clear();

        BaselineTotal = 0f;
        RemainingTotal = 0f;

        weakMonsterSpawnCount = 0;
        LastStep = 0;

        HasInitializedTargets = false;
        clearedAllGas = false;
        isStageReady = false;
        ignoreFirstStepAfterSetup = true;

        isStageCompleted = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            if (HasStateAuthority)
                ForceCompleteStageByCheat();
            else
                RPC_RequestForceCompleteStageByCheat();

            return;
        }

        // 단계 이벤트는 권한 쪽에서만 처리
        if (!HasStateAuthority) return;

        if (isStageReady && !isInitializingTargets && !isStageCompleted)
        {
            int interval = GetStepInterval();
            int currentStep = Mathf.FloorToInt(ClearPercent / (float)interval) * interval;
            currentStep = Mathf.Clamp(currentStep, 0, 100);

            if (ignoreFirstStepAfterSetup)
            {
                ignoreFirstStepAfterSetup = false;
                LastStep = currentStep;
                return;
            }

            if (ClearPercent <= 0 && LastStep >= 100)
            {
                LastStep = 0;
            }

            // cheatScene이면 퍼센트별 이벤트 실행 안 함
            if (!cheatScene)
            {
                CheckStep(ClearRatio01);
            }
            else
            {
                LastStep = currentStep;
            }

            if (!clearedAllGas && ClearPercent >= 100)
            {
                CompleteStage();
            }
        }

        if (Input.GetKeyDown(KeyCode.F1)) SpawnWatcher();
        if (Input.GetKeyDown(KeyCode.F2)) SpawnBoxHead();
        if (Input.GetKeyDown(KeyCode.F3)) SpawnLegless();
        if (Input.GetKeyDown(KeyCode.F4)) PollutionSpawner.Instance.SpawnGas();
        if (Input.GetKeyDown(KeyCode.F5)) SpawnRatOnly();
        if (Input.GetKeyDown(KeyCode.L)) SpawnRoachOnly();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestForceCompleteStageByCheat()
    {
        ForceCompleteStageByCheat();
    }

    public void ForceCompleteStageByCheat()
    {
        if (!HasStateAuthority) return;
        if (isStageCompleted) return;

        if (BaselineTotal <= 0f)
            BaselineTotal = 1f;

        HasInitializedTargets = true;
        isInitializingTargets = false;
        isStageReady = true;
        ignoreFirstStepAfterSetup = false;

        RemainingTotal = 0f;
        LastStep = 100;

        CompleteStage();
    }

    private void CompleteStage()
    {
        if (!HasStateAuthority) return;
        if (clearedAllGas) return;

        clearedAllGas = true;
        isStageCompleted = true;

        DespawnAllSpawnedDangerObjects();

        // 오염물질 삭제
        if (PollutionSpawner.Instance != null)
        {
            PollutionSpawner.Instance.DespawnAllCleaningObjects();

            if (PollutionSpawner.Instance.cleaningTargets != null)
                Destroy(PollutionSpawner.Instance.cleaningTargets);
        }

        if (clearDoorAnim != null)
            clearDoorAnim.TryOpenDoor();
    }

    private void RegisterSpawnedDangerObject(NetworkObject obj)
    {
        if (obj == null) return;

        if (!spawnedDangerObjects.Contains(obj))
            spawnedDangerObjects.Add(obj);
    }

    private void RegisterSpawnedDangerObjects(List<NetworkObject> objs)
    {
        if (objs == null) return;

        for (int i = 0; i < objs.Count; i++)
        {
            RegisterSpawnedDangerObject(objs[i]);
        }
    }

    private void DespawnAllSpawnedDangerObjects()
    {
        if (!HasStateAuthority) return;

        for (int i = spawnedDangerObjects.Count - 1; i >= 0; i--)
        {
            NetworkObject obj = spawnedDangerObjects[i];

            if (obj != null)
                Runner.Despawn(obj);
        }

        spawnedDangerObjects.Clear();
        weakMonsterSpawnCount = 0;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (!debuffInitialized)
        {
            debuffInitialized = true;

            SetupRePollutionDebuff();
        }
    }

    // 스테이지 / 잔향에 따라 step 퍼센트 변경
    int GetStepInterval()
    {
        // 기본 퍼센트
        int defaultInterval = 10;

        // FastStep 아니면 기본 유지
        if (currentDebuff != RePollutionDebuffType.FastStep)
            return defaultInterval;

        // 스테이지별 간격 축소
        if (stageNum < 4)
            return 8;

        if (stageNum < 7)
            return 7;

        if (stageNum < 10)
            return 6;

        return 10;
    }

    // 중첩 될 확률
    float GetDoubleEventChance()
    {
        if (stageNum < 4)
            return 0.15f;

        if (stageNum < 7)
            return 0.30f;

        if (stageNum < 10)
            return 0.45f;

        return 0f;
    }

    // 10% 단위로 step으로 변환
    void CheckStep(float clearRatio01)
    {
        int interval = GetStepInterval();

        int step = Mathf.FloorToInt(ClearPercent / (float)interval) * interval;

        step = Mathf.Clamp(step, 0, 100);

        if (step <= LastStep) return;

        for (int s = LastStep + interval; s <= step; s += interval)
        {
            if (s < interval || s > 90)
                continue;

            bool triggeredDouble = false;

            // 중첩 디버프 확률 체크
            if (currentDebuff == RePollutionDebuffType.DoubleEvent)
            {
                float chance = GetDoubleEventChance();

                if (Random.value <= chance)
                {
                    triggeredDouble = true;
                }
            }

            // 중첩 실행
            if (triggeredDouble)
                RunDoubleRandomFromPhase(s);
            else
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
        ExecuteAction(pick);
    }

    // 중첩 전용 랜덤 실행 (같은 이벤트 중복X)
    void RunDoubleRandomFromPhase(int step)
    {
        int phaseTier = GetPhaseTier(step);

        if ((phaseTier == 2 || phaseTier == 3) &&
            weakMonsterSpawnCount >= 2)
        {
            ForceSpawnStrongByPhase(phaseTier);
            return;
        }

        PhaseRandom phase = FindPhase(step);

        if (phase == null) return;
        if (phase.actions == null) return;
        if (phase.actions.Count <= 1)
        {
            RunRandomFromPhase(step);
            return;
        }

        // 이벤트 복사용 (이벤트를 복사 해 두고 그안에서 랜덤으로 나오는데 그 중에서 첫번째로 나오는건 리스트에서 제거)
        List<RandomAction> available = new List<RandomAction>(phase.actions);

        // 첫 번째 선택
        RandomAction first = PickWeightedRandom(available);

        if (first == null)
            return;

        ExecuteAction(first);

        // 첫 번째로 뽑힌 이벤트 다음 후보에서 제거
        available.Remove(first);

        // 남은것들 중 두 번째 선택
        RandomAction second = PickWeightedRandom(available);

        if (second == null)
            return;

        ExecuteAction(second);
    }

    void ExecuteAction(RandomAction action)
    {
        if (action == null) return;

        bool hasInvoke = action.onInvoke != null && action.onInvoke.GetPersistentEventCount() > 0;

        if (hasInvoke)
        {
            action.onInvoke.Invoke();
            ClassifyAndRecordAction(action);
            return;
        }

        if (action.prefab.IsValid)
        {
            SpawnFromActionPoint(action);
            ClassifyAndRecordAction(action);
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
        if (isStageCompleted) return;
        if (action == null) return;
        if (!action.prefab.IsValid) return;

        Transform point = action.spawnPoint != null ? action.spawnPoint : spawnRoot;
        if (point == null) point = transform;

        NetworkObject obj = Runner.Spawn(action.prefab, point.position, point.rotation);
        RegisterSpawnedDangerObject(obj);
    }

    // 플레이어 근처 소환
    public void SpawnOneNearPlayer(NetworkPrefabRef prefab)
    {
        if (!HasStateAuthority) return;
        if (isStageCompleted) return;
        if (!prefab.IsValid) return;

        Transform player = FindAnyPlayerTransform();
        if (player == null) return;

        if (TryGetSpawnPositionNearTarget(player, out Vector3 spawnPos))
        {
            NetworkObject obj = Runner.Spawn(prefab, spawnPos, Quaternion.identity);
            RegisterSpawnedDangerObject(obj);
        }
    }

    // 무조건 스폰할때까지 도전
    IEnumerator SpawnOneNearPlayerUntilSuccess(NetworkPrefabRef prefab)
    {
        if (!HasStateAuthority) yield break;
        if (!prefab.IsValid) yield break;

        while (!isStageCompleted)
        {
            Transform player = FindAnyPlayerTransform();

            if (player != null)
            {
                if (TryGetSpawnPositionNearTarget(player, out Vector3 spawnPos))
                {
                    NetworkObject obj = Runner.Spawn(prefab, spawnPos, Quaternion.identity);
                    RegisterSpawnedDangerObject(obj);
                    yield break;
                }
            }

            yield return null;
        }
    }

    // 스테이지 시작 전 초기화
    public void FinishStageSetup()
    {
        if (!HasStateAuthority) return;

        RemainingTotal = BaselineTotal;

        isInitializingTargets = false;
        HasInitializedTargets = true;

        LastStep = 0;
        isStageReady = true;
        ignoreFirstStepAfterSetup = false;
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
        if (isStageCompleted) return;

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
            List<NetworkObject> objs = PollutionSpawner.Instance.SpawnRoachesInRandomAreas(roach, count);
            RegisterSpawnedDangerObjects(objs);
        }
    }

    // 혼합 스폰 (쥐는 무조건 1마리 스폰)
    public void SpawnMixed()
    {
        if (!HasStateAuthority) return;
        if (isStageCompleted) return;

        List<RandomAction> spawnableRoachActions = GetSpawnableActions(ratSpawnActions);
        if (spawnableRoachActions.Count == 0) return;

        RandomAction baseAction = PickWeightedRandom(spawnableRoachActions);
        if (baseAction == null) return;

        int ratCount = 1;
        int roachCount = PickSpawnCount(baseAction, 1);

        Debug.Log("Mix");

        SpawnFixedRats(ratCount);

        if (PollutionSpawner.Instance != null)
        {
            List<NetworkObject> objs = PollutionSpawner.Instance.SpawnRoachesInRandomAreas(roach, roachCount);
            RegisterSpawnedDangerObjects(objs);
        }
    }

    public void SpawnBoxHead()
    {
        if (!HasStateAuthority) return;
        if (isStageCompleted) return;

        Debug.Log("BoxHead");
        StartCoroutine(SpawnOneNearPlayerUntilSuccess(boxHead));
    }

    public void SpawnLegless()
    {
        if (!HasStateAuthority) return;
        if (isStageCompleted) return;

        Debug.Log("Legless");
        StartCoroutine(SpawnOneNearPlayerUntilSuccess(legless));
    }

    public void SpawnWatcher()
    {
        if (!HasStateAuthority) return;
        if (isStageCompleted) return;

        Debug.Log("Watcher");

        if (PollutionSpawner.Instance != null)
        {
            List<NetworkObject> objs = PollutionSpawner.Instance.SpawnRoachesInRandomAreas(watcher, 1);
            RegisterSpawnedDangerObjects(objs);
        }
    }

    /// <summary>
    /// 여기까지 모음집
    /// </summary>
    
    // 현재 진행도가 무슨 step인지
    int GetPhaseTier(int step)
    {
        if (currentDebuff == RePollutionDebuffType.ChaosPhase)
            return 3;

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
        if (currentDebuff == RePollutionDebuffType.ChaosPhase)
        {
            step = 70;
        }

        for (int i = 0; i < phases.Count; i++)
        {
            var p = phases[i];

            if (p == null)
                continue;

            if (step < p.minStep || step > p.maxStep)
                continue;

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