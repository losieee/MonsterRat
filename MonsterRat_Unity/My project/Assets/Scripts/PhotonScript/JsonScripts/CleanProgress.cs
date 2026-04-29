using System.Collections.Generic;

[System.Serializable]
public class CleanProgress
{
    public int Stg_Num;
    public float StgStainProgress;
    public float StgTrashProgress;
    public float StgPipeProgress;
}

[System.Serializable]
public class StageProgressDataList
{
    public List<CleanProgress> stages;
}
