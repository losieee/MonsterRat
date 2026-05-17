using UnityEngine;
using Fusion;

public class WorldLoadManager : NetworkBehaviour
{
    public Transform[] itemSpawnPoints;

    public override void Spawned()
    {
        // 방장만 아이템을 스폰
        if (!HasStateAuthority) return;

        string json = PlayerPrefs.GetString("MasterWorldSave", "");

        if (!string.IsNullOrEmpty(json))
        {
            WorldSaveData data = JsonUtility.FromJson<WorldSaveData>(json);

            //문 상태 복구
            TransStageDoor.isPollutionLeft = data.isDoorActive;

            // 2. 바닥 아이템들을 지정된 위치에 순서대로 스폰
            if (itemSpawnPoints != null && itemSpawnPoints.Length > 0)
            {
                int spawnIndex = 0; // 스폰할 위치 번호

                foreach (var sItem in data.shipItems)
                {
                    ItemData itemData = ItemDatabase.Instance.GetItem(sItem.itemID);
                    if (itemData != null && itemData.itemPrefab != null)
                    {
                        NetworkObject prefabNetObj = itemData.itemPrefab.GetComponent<NetworkObject>();

                        // %itemSpawnPoints.Length 를 쓰면, 아이템이 스폰 포인트 개수보다 많아도 다시 0번부터 겹쳐서 소환
                        Transform currentPoint = itemSpawnPoints[spawnIndex % itemSpawnPoints.Length];

                        Vector3 spawnPos = currentPoint.position;
                        Quaternion spawnRot = currentPoint.rotation;

                        Runner.Spawn(prefabNetObj, spawnPos, spawnRot);

                        spawnIndex++; // 다음 아이템은 다음 자리(Empty Object)로!
                    }
                }
              //  Debug.Log($"[WorldLoad] 가져온 아이템 {data.shipItems.Count}개를 지정된 스폰 위치에 배치했습니다!");
            }
            else
            {
                //Debug.LogWarning("[WorldLoad] 에러! 인스펙터에 itemSpawnPoints(스폰 자리)가 한 개도 설정되지 않았습니다.");
            }
        }
    }
}