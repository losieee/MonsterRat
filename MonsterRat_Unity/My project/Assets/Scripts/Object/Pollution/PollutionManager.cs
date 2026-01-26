using UnityEngine;
using UnityEngine.UI;

public class PollutionManager : MonoBehaviour
{
    public static PollutionManager Instance;

    [Header("UI")]
    public Image clearGaugeFill;

    [Header("Count")]
    public int totalSpawned = 0;
    public int cleanedCount = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        UpdateGauge();
    }

    // 오염 오브젝트 생성 될 때
    public void RegisterPollution()
    {
        totalSpawned++;
        UpdateGauge();
    }

    // 오염 오브젝트 제거 될 때
    public void OnPollutionCleaned()
    {
        cleanedCount++;
        UpdateGauge();
    }

    void UpdateGauge()
    {
        if (clearGaugeFill == null)
            return;

        if (totalSpawned <= 0)
        {
            clearGaugeFill.fillAmount = 0f;
            return;
        }

        float fill = (float)cleanedCount / totalSpawned;
        clearGaugeFill.fillAmount = Mathf.Clamp01(fill);
    }
}
