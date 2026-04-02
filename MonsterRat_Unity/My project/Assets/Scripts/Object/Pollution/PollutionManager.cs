using UnityEngine;
using Fusion;

public class PollutionManager : NetworkBehaviour
{
    public static PollutionManager Instance { get; private set; }

    [Networked]
    public int TotalSpawned { get; set; }

    [Networked]
    public int CleanedCount { get; set; }

    public float FillAmount
    {
        get
        {
            if (TotalSpawned <= 0)
                return 0f;

            return Mathf.Clamp01((float)CleanedCount / TotalSpawned);
        }
    }

    public int FillPercent
    {
        get
        {
            return Mathf.RoundToInt(FillAmount * 100f);
        }
    }

    public override void Spawned()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // 오염 오브젝트 생성 시 호출
    public void RegisterPollution()
    {
        if (!HasStateAuthority)
            return;

        TotalSpawned++;
    }

    // 오염 오브젝트 청소 완료 시 호출
    public void OnPollutionCleaned()
    {
        if (!HasStateAuthority)
            return;

        CleanedCount++;
    }

    // 필요하면 스테이지 시작 시 초기화용
    public void ResetPollution()
    {
        if (!HasStateAuthority)
            return;

        TotalSpawned = 0;
        CleanedCount = 0;
    }
}