using System.Collections.Generic;
using UnityEngine;

public class StageProgressManager : MonoBehaviour
{
    public static StageProgressManager Instance;

    [Header("ÁøÇàµµ Json")]
    [SerializeField] private TextAsset stageProgressJson;
    [SerializeField] private int currentStageNum = 1;

    private List<CleanProgress> progressList = new List<CleanProgress>();
    private CleanProgress currentData;

    void Awake()
    {
        Instance = this;

        LoadProgressData();
        ApplyCurrentStage(currentStageNum);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void LoadProgressData()
    {
        if (stageProgressJson == null)
            return;

        string wrappedJson = "{\"stages\":" + stageProgressJson.text + "}";

        StageProgressDataList dataList = JsonUtility.FromJson<StageProgressDataList>(wrappedJson);

        if (dataList == null || dataList.stages == null)
            return;

        progressList = dataList.stages;
    }

    void ApplyCurrentStage(int stageNum)
    {
        currentData = progressList.Find(data => data.Stg_Num == stageNum);

        if (currentData == null)
            return;
    }

    public float GetWeight(ClearTargetType type)
    {
        if (currentData == null)
            return 1f;

        switch (type)
        {
            case ClearTargetType.Pollution:
                return currentData.StgStainProgress;

            case ClearTargetType.Trash:
                return currentData.StgTrashProgress;

            case ClearTargetType.Gas:
                return currentData.StgPipeProgress;
        }

        return 1f;
    }
}
