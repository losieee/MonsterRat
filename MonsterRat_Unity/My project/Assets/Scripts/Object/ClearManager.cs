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
        public GameObject prefab;
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

    public Transform spawnRoot;
    public List<PhaseRandom> phases = new List<PhaseRandom>();

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

    void RunRandomFromPhase(int step)
    {
        PhaseRandom phase = FindPhase(step);
        if (phase == null) return;
        if (phase.actions == null || phase.actions.Count == 0) return;

        RandomAction pick = PickWeightedRandom(phase.actions);
        if (pick == null) return;

        if (pick.prefab != null)
        {
            Transform point = pick.spawnPoint != null ? pick.spawnPoint : spawnRoot;
            if (point == null) point = transform;

            Instantiate(pick.prefab, point.position, point.rotation);
        }

        pick.onInvoke?.Invoke();
    }

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

            bool hasSomething = (a.prefab != null) || (a.onInvoke != null);
            if (!hasSomething) continue;

            total += Mathf.Max(0f, a.weight);
        }

        if (total <= 0f) return null;

        float r = Random.Range(0f, total);

        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null) continue;

            bool hasSomething = (a.prefab != null) || (a.onInvoke != null);
            if (!hasSomething) continue;

            float w = Mathf.Max(0f, a.weight);
            r -= w;

            if (r < 0f)
                return a;
        }

        return null;
    }
}