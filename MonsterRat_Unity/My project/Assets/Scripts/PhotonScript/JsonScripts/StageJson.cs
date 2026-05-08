using System.Collections.Generic;

[System.Serializable]
public class StageJson
{
    public int Stg_Num;
    public int Stg_Stain;
    public int Stg_Trash;
    public int Stg_Pipe;
    public int Stg_DifficullyTier;
    public int Stg_Total;
    public string Stg_MapType;
}

[System.Serializable]
public class StageDataList
{
    public List<StageJson> stages;
}