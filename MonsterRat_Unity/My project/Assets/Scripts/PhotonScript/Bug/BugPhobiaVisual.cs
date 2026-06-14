using UnityEngine;
using Fusion;

public class BugPhobiaVisual : NetworkBehaviour
{
    [Header("모델링 연결 (Animator 포함)")]
    public GameObject scaryBugModel;      // 이게 원본
    public GameObject censoredModel;      // 이게 스폰지밥

    private void Start()
    {

        //오프라인도 가능하긴해요 
        ApplyLocalVisual();
        SlimUI.ModernMenu.UISettingsManager.OnSettingsUpdated -= ApplyLocalVisual; // 중복 방지
        SlimUI.ModernMenu.UISettingsManager.OnSettingsUpdated += ApplyLocalVisual;
    }

    // 온라인 환경 스폰 시 작동
    public override void Spawned()
    {
        ApplyLocalVisual();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        SlimUI.ModernMenu.UISettingsManager.OnSettingsUpdated -= ApplyLocalVisual;
    }

    private void OnDestroy()
    {
        SlimUI.ModernMenu.UISettingsManager.OnSettingsUpdated -= ApplyLocalVisual;
    }

    private void ApplyLocalVisual()
    {
        int phobiaMode = 0;
        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
        {
            phobiaMode = SlimUI.ModernMenu.UISettingsManager.Instance.BugPhobiaMode;
        }
        else
        {
            string jsonString = PlayerPrefs.GetString("MasterGameSettings", "");
            if (!string.IsNullOrEmpty(jsonString))
            {
                SlimUI.ModernMenu.GameSettingsData data = JsonUtility.FromJson<SlimUI.ModernMenu.GameSettingsData>(jsonString);
                phobiaMode = data.bugPhobiaMode;
            }
        }
        bool isCensored = (phobiaMode == 1);

        scaryBugModel.SetActive(!isCensored);
        censoredModel.SetActive(isCensored);
    }
}