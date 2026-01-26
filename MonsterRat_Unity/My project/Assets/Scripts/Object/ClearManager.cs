using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IClearTarget
{
    // 1 - 아직 남아있음, 0 - 다 제거됨
    float Remain01 { get; }
}

public class ClearManager : MonoBehaviour
{
    public static ClearManager Instance;
    public Image clearGaugeFill;
    public Text clearGaugeText;

    // 클리어 대상 목록
    private readonly List<IClearTarget> targets = new List<IClearTarget>();
    private float baselineTotal = 0f;

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

        if (clearGaugeFill != null)
            clearGaugeFill.fillAmount = 0f;
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
        if (clearGaugeFill == null) return;
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

        // 시작은 무조건 0
        clearGaugeFill.fillAmount = clearRatio;
        clearGaugeText.text = $"{clearRatio.ToString("F2")}%";
    }
}
