using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class WorldLoadManager : NetworkBehaviour
{
    [Header("이 스테이지 진입 시 지급할 골드")]
    public int currentStageReward = 500;  //이게 스테이지별 얼마씩 줄건지 적는 변수

    public Transform[] itemSpawnPoints;

    public static WorldLoadManager instance;
    [Networked] public int SharedGold { get; set; }

    private void Awake()
    {
        instance = this;
    }

    public override void Spawned()
    {
       
        if (!HasStateAuthority) return;
        string json = PlayerPrefs.GetString("MasterWorldSave", "");
        if (!string.IsNullOrEmpty(json))
        {
            WorldSaveData data = JsonUtility.FromJson<WorldSaveData>(json);

            // 문 상태 복구
            TransStageDoor.isPollutionLeft = data.isDoorActive;
            SharedGold = data.currentGold; 
            if (!data.isStageStartGoldClaimed) 
            {
                SharedGold += currentStageReward;    // 남은 돈 + 인스펙터 설정값 더하기
                data.currentGold = SharedGold;       // 세이브 장부에 최신 골드 기록
                data.isStageStartGoldClaimed = true; // 돈 중복 입금 금지

                // 즉시 세이브 파일을 덮어써서 게임을 껐다 켜도 돈이 중복으로 안 들어오게 방지
                string updatedJson = JsonUtility.ToJson(data);
                PlayerPrefs.SetString("MasterWorldSave", updatedJson);
                int activeSlot = PlayerPrefs.GetInt("CurrentActiveSaveSlot", 0);
                PlayerPrefs.SetString("SaveSlot_" + activeSlot, updatedJson);
                PlayerPrefs.Save();
                
            }

            //바닥 아이템들을 지정된 위치에 순서대로 스폰
            if (itemSpawnPoints != null && itemSpawnPoints.Length > 0 && data.shipItems != null)
            {
                int spawnIndex = 0; 
                foreach (var sItem in data.shipItems)
                {
                    ItemData itemData = ItemDatabase.Instance.GetItem(sItem.itemID);
                    if (itemData != null && itemData.itemPrefab != null)
                    {
                        NetworkObject prefabNetObj = itemData.itemPrefab.GetComponent<NetworkObject>();
                        Transform currentPoint = itemSpawnPoints[spawnIndex % itemSpawnPoints.Length];
                        Runner.Spawn(prefabNetObj, currentPoint.position, currentPoint.rotation);
                        spawnIndex++; 
                    }
                }
            }
        }
        else
        {
            // 세이브 파일이 아예 없는 1스테이지 최초 시작 시
            SharedGold = currentStageReward;
            
            WorldSaveData newData = new WorldSaveData();
            newData.currentGold = SharedGold;
            newData.isStageStartGoldClaimed = true;
            newData.savedStageName = SceneManager.GetActiveScene().name;

            int activeSlot = PlayerPrefs.GetInt("CurrentActiveSaveSlot", 0);
            string existingSlotJson = PlayerPrefs.GetString("SaveSlot_" + activeSlot, "");
            if (!string.IsNullOrEmpty(existingSlotJson))
            {
                SaveSlotData slotData = JsonUtility.FromJson<SaveSlotData>(existingSlotJson);
                newData.roomName = slotData.roomName; // 로비에서 입력했던 방 이름 복구
            }


            string newJson = JsonUtility.ToJson(newData);
            PlayerPrefs.SetString("MasterWorldSave", newJson);
            PlayerPrefs.SetString("SaveSlot_" + activeSlot, newJson);
            PlayerPrefs.Save();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_DeductGold(int amount)
    {
        SharedGold -= amount;
    }
}