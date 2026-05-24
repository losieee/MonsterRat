using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ShipItemData
{
    public int itemID;
    public Vector3 localPosition;
    public Quaternion localRotation;
}
[System.Serializable]
public class WorldSaveData
{
    public string savedStageName;
    public string roomName;        
    public bool isDoorActive;
    public int currentGold;
    public List<ShipItemData> shipItems = new List<ShipItemData>();
}