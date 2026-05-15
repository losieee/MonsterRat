using UnityEngine;
using Fusion;

public class WorldLoadManager : NetworkBehaviour
{
    [Header("현재 스테이지의 함선 기준점")]
    public Transform currentShipCenter;

    public override void Spawned()
    {
        //방장만 권한이 있음
        if (!HasStateAuthority) return;

        string json = PlayerPrefs.GetString("MasterWorldSave", "");

        if (!string.IsNullOrEmpty(json))
        {
            WorldSaveData data = JsonUtility.FromJson<WorldSaveData>(json);
            TransStageDoor.isPollutionLeft = data.isDoorActive;

            if (currentShipCenter != null)
            {
                foreach (var sItem in data.shipItems)
                {
                    ItemData itemData = ItemDatabase.Instance.GetItem(sItem.itemID);
                    if (itemData != null && itemData.itemPrefab != null)
                    { 
                        NetworkObject prefabNetObj = itemData.itemPrefab.GetComponent<NetworkObject>();

                        Vector3 spawnPos = currentShipCenter.TransformPoint(sItem.localPosition);
                        Quaternion spawnRot = currentShipCenter.rotation * sItem.localRotation;

                        Runner.Spawn(prefabNetObj, spawnPos, spawnRot);
                    }
                }
               // Debug.Log($"[WorldLoad] 이전 스테이지에서 가져온 아이템 {data.shipItems.Count}개를 스폰했습니다.");
            }
        }
    }
}