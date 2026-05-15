using UnityEngine;
using Fusion;

public class WorldLoadManager : NetworkBehaviour
{
    [Header("현재 스테이지의 함선 기준점")]
    public Transform currentShipCenter;

    public override void Spawned()
    {
        // 월드 생성(아이템 드랍)은 방장만 권한이 있습니다.
        if (!HasStateAuthority) return;

        string json = PlayerPrefs.GetString("MasterWorldSave", "");

        if (!string.IsNullOrEmpty(json))
        {
            WorldSaveData data = JsonUtility.FromJson<WorldSaveData>(json);

            // 1. 방장이 TransStageDoor의 값 업데이트를 위해 전역 변수 세팅 (문 활성화 연동)
            TransStageDoor.isPollutionLeft = data.isDoorActive;

            // 2. 바닥에 저장되었던 아이템들 스폰
            if (currentShipCenter != null)
            {
                foreach (var sItem in data.shipItems)
                {
                    ItemData itemData = ItemDatabase.Instance.GetItem(sItem.itemID);
                    if (itemData != null && itemData.itemPrefab != null)
                    { 
                        NetworkObject prefabNetObj = itemData.itemPrefab.GetComponent<NetworkObject>();

                        // 아까 저장한 '상대 좌표'를 현재 스테이지 함선 기준의 '절대 좌표'로 다시 풉니다!
                        Vector3 spawnPos = currentShipCenter.TransformPoint(sItem.localPosition);
                        Quaternion spawnRot = currentShipCenter.rotation * sItem.localRotation;

                        Runner.Spawn(prefabNetObj, spawnPos, spawnRot);
                    }
                }
                Debug.Log($"[WorldLoad] 이전 스테이지에서 가져온 아이템 {data.shipItems.Count}개를 스폰했습니다.");
            }
        }
    }
}