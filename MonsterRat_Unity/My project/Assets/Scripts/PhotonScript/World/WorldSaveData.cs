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
    public string roomName;        // ★ 추가: 방 이름을 잃어버리지 않기 위함
    public bool isDoorActive;
    public List<ShipItemData> shipItems = new List<ShipItemData>();
}