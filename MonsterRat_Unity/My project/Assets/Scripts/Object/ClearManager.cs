using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public interface IClearTarget
{
    // 1-아직 남아있음, 0-다 제거됨
    float Remain01 { get; }
}

public class ClearManager : MonoBehaviour
{
    public static ClearManager Instance;
    public Image clearGaugeFill;
    public Text clearGaugeText;

    [System.Serializable]
    public class StepSpawn
    {
        [Range(10, 90)] public int step;
        public GameObject spawnPrefab;
        public Transform spawnPoint;
        public bool spawnOnce = true;
    }

    public Transform spawnRoot;
    public List<StepSpawn> stepSpawns = new List<StepSpawn>();

    [System.Serializable]
    public class StepEvent
    {
        [Range(10, 90)] public int step;
        public UnityEvent onReached;
        public bool invokeOnce = true;
    }

    public List<StepEvent> stepEvents = new List<StepEvent>();

    // 클리어 대상 목록
    private readonly List<IClearTarget> targets = new List<IClearTarget>();
    private float baselineTotal = 0f;

    private int lastStep = 0;
    private readonly HashSet<int> spawnedSteps = new HashSet<int>();
    private readonly HashSet<int> invokedSteps = new HashSet<int>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    IEnumerator Start()
    {
        yield return null;

        baselineTotal = targets.Count;
        if (baselineTotal <= 0) baselineTotal = 1;

        lastStep = 0;
        spawnedSteps.Clear();
        invokedSteps.Clear();

        if (clearGaugeFill != null)
            clearGaugeFill.fillAmount = 0f;

        if (clearGaugeText != null)
            clearGaugeText.text = "0%";

        if (spawnRoot == null)
            spawnRoot = transform;
    }

    void Update()
    {
        UpdateGauge();
    }

    // 오브젝트가 생성될 때
    public void Register(IClearTarget target)
    {
        if (target == null) return;
        if (targets.Contains(target)) return;
        targets.Add(target);

        baselineTotal += 1f;
    }

    // 오브젝트가 사라질 때
    public void Unregister(IClearTarget target)
    {
        if (target == null) return;
        targets.Remove(target);
    }

    // 게이지 계산
    void UpdateGauge()
    {
        if (baselineTotal <= 0f) return;

        float remainTotal = 0f;

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i] == null)
            {
                targets.RemoveAt(i);
                continue;
            }

            remainTotal += Mathf.Clamp01(targets[i].Remain01);
        }

        // 진행률 = 1 - (현재 남은량 / 처음 총량)
        float clearRatio = 1f - (remainTotal / baselineTotal);
        clearRatio = Mathf.Clamp01(clearRatio);

        if (clearGaugeFill != null)
            clearGaugeFill.fillAmount = clearRatio;

        if (clearGaugeText != null)
            clearGaugeText.text = $"{(clearRatio * 100f):F0}%";

        CheckStep(clearRatio);
    }

    void CheckStep(float clearRatio01)
    {
        // 0~100 중 10단위로 내림
        int step = Mathf.FloorToInt(clearRatio01 * 10f) * 10;
        step = Mathf.Clamp(step, 0, 100);

        if (step <= lastStep) return;

        // 10단위로 무조건 처리
        for (int s = lastStep + 10; s <= step; s += 10)
        {
            if (s >= 10 && s <= 90)
            {
                TrySpawnAtStep(s);
                TryInvokeStepEvent(s);
            }
        }

        lastStep = step;
    }

    void TrySpawnAtStep(int step)
    {
        // stepSpawns에서 해당 step 찾기
        for (int i = 0; i < stepSpawns.Count; i++)
        {
            var entry = stepSpawns[i];
            if (entry == null) continue;
            if (entry.step != step) continue;
            if (entry.spawnPrefab == null) continue;

            // 중복 방지
            if (entry.spawnOnce && spawnedSteps.Contains(step))
                return;

            Transform point = entry.spawnPoint != null ? entry.spawnPoint : spawnRoot;
            if (point == null) point = transform;

            Instantiate(entry.spawnPrefab, point.position, point.rotation);

            if (entry.spawnOnce)
                spawnedSteps.Add(step);

            return;
        }
    }

    // 이벤트 소환
    void TryInvokeStepEvent(int step)
    {
        for (int i = 0; i < stepEvents.Count; i++)
        {
            var entry = stepEvents[i];
            if (entry == null) continue;
            if (entry.step != step) continue;

            if (entry.invokeOnce && invokedSteps.Contains(step))
                return;

            entry.onReached?.Invoke();

            if (entry.invokeOnce)
                invokedSteps.Add(step);

            return;
        }
    }
}
