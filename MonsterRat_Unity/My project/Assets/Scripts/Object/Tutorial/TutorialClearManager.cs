using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public interface TutorialIClearTarget
{
    // 1-아직 남아있음, 0-다 제거됨
    float Remain01 { get; }

    // 게이지에 차지하는 비중
    float Weight { get; }
}

public class TutorialClearManager : MonoBehaviour
{
    public static TutorialClearManager Instance;
    public Image clearGaugeFill;
    public Text clearGaugeText;

    [System.Serializable]
    public class RandomAction
    {
        [Header("Objects")]
        public GameObject prefab;      // 프리팹 소환
        public Transform spawnPoint;

        [Header("Events")]
        public UnityEvent onInvoke;    // 다른 스크립트 함수 호출

        [Header("Percent")]
        public float weight = 1;         // 확률
    }

    [System.Serializable]
    public class PhaseRandom
    {
        public string name;
        [Range(0, 90)] public int minStep;
        [Range(0, 90)] public int maxStep;
        public List<RandomAction> actions = new List<RandomAction>();        // 랜덤하게 실행 할 목록
    }

    public Transform spawnRoot;
    public List<PhaseRandom> phases = new List<PhaseRandom>();

    // 클리어 대상 목록
    private readonly List<TutorialIClearTarget> targets = new List<TutorialIClearTarget>();
    private float baselineTotal = 0f;
    private int lastStep = 0;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindUI();
    }

    IEnumerator Start()
    {
        BindUI();
        yield return null;

        RebuildBaseline();

        lastStep = 0;

        if (clearGaugeFill != null) clearGaugeFill.fillAmount = 0f;
        if (clearGaugeText != null) clearGaugeText.text = "0%";
        if (spawnRoot == null) spawnRoot = transform;
    }

    void BindUI()
    {
        if (clearGaugeFill != null && clearGaugeText != null) return;

        // DontDestroy된 Player에서 PlayerHUD 찾기
        PlayerHUD hud = FindFirstObjectByType<PlayerHUD>();
        if (hud == null) return;

        clearGaugeFill = hud.clearGaugeFill;
        clearGaugeText = hud.clearGaugeText;
    }

    void Update()
    {
        UpdateGauge();
    }

    // 오브젝트가 생성될 때
    public void Register(TutorialIClearTarget target)
    {
        if (target == null) return;
        if (targets.Contains(target)) return;
        targets.Add(target);

        baselineTotal += Mathf.Max(0f, target.Weight);
        if (baselineTotal <= 0f) baselineTotal = 1f;
    }

    // 오브젝트가 사라질 때
    public void Unregister(TutorialIClearTarget target)
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

            float w = Mathf.Max(0f, targets[i].Weight);
            remainTotal += Mathf.Clamp01(targets[i].Remain01) * w;
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

    // weight에 맞게 게이지 다시 계산
    public void RebuildBaseline()
    {
        baselineTotal = 0f;
        for (int i = 0; i < targets.Count; i++)
            baselineTotal += Mathf.Max(0f, targets[i].Weight);

        if (baselineTotal <= 0f) baselineTotal = 1f;
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
                RunRandomFromPhase(s);
            }
        }
        lastStep = step;
    }

    // 랜덤 소환
    void RunRandomFromPhase(int step)
    {
        PhaseRandom phase = FindPhase(step);
        if (phase == null) return;
        if (phase.actions == null || phase.actions.Count == 0) return;

        RandomAction pick = PickWeightedRandom(phase.actions);
        if (pick == null) return;

        // 프리팹 소환
        if (pick.prefab != null)
        {
            Transform point = pick.spawnPoint != null ? pick.spawnPoint : spawnRoot;
            if (point == null) point = transform;

            Instantiate(pick.prefab, point.position, point.rotation);
        }

        // 이벤트 실행
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
        float total = 0;

        // 총 확률의 합
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null) continue;

            // 프리팹도 없고 이벤트도 없으면 후보에서 제외
            bool hasSomething = (a.prefab != null) || (a.onInvoke != null);
            if (!hasSomething) continue;

            total += Mathf.Max(0, a.weight);
        }

        if (total <= 0) return null;

        float r = Random.Range(0, total);

        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null) continue;

            bool hasSomething = (a.prefab != null) || (a.onInvoke != null);
            if (!hasSomething) continue;

            float w = Mathf.Max(0, a.weight);
            r -= w;
            if (r < 0) return a;
        }

        return null;
    }
}
