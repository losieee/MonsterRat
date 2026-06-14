using UnityEngine;

public class BugPhobiaVisual : MonoBehaviour
{
    public GameObject scaryBugModel;
    public GameObject censoredModel;

    private void Start()
    {
        ApplyLocalVisual();
        SlimUI.ModernMenu.UISettingsManager.OnSettingsUpdated -= ApplyLocalVisual;
        SlimUI.ModernMenu.UISettingsManager.OnSettingsUpdated += ApplyLocalVisual;
    }

    private void OnDestroy()
    {
        SlimUI.ModernMenu.UISettingsManager.OnSettingsUpdated -= ApplyLocalVisual;
    }

    private void ApplyLocalVisual()
    {
        if (scaryBugModel == null || censoredModel == null) return;

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