using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class WorldLoadManager : NetworkBehaviour
{
    [Header("이 스테이지 진입 시 지급할 골드")]
    public int currentStageReward = 500;

    public Transform[] itemSpawnPoints;

    public static WorldLoadManager instance;
    [Networked] public int SharedGold { get; set; }

    private void Awake() { instance = this; }

    public override void Spawned()
    {
        if (!HasStateAuthority) return;

        string json = PlayerPrefs.GetString("MasterWorldSave", "");
        if (!string.IsNullOrEmpty(json))
        {
            WorldSaveData data = JsonUtility.FromJson<WorldSaveData>(json);

            TransStageDoor.isPollutionLeft = data.isDoorActive;
            SharedGold = data.currentGold;
            if (!data.isStageStartGoldClaimed)
            {
                SharedGold += currentStageReward;
                data.currentGold = SharedGold;
                data.isStageStartGoldClaimed = true;

                string updatedJson = JsonUtility.ToJson(data);
                PlayerPrefs.SetString("MasterWorldSave", updatedJson);
                int activeSlot = PlayerPrefs.GetInt("CurrentActiveSaveSlot", 0);
                PlayerPrefs.SetString("SaveSlot_" + activeSlot, updatedJson);
                PlayerPrefs.Save();
            }

            if (itemSpawnPoints != null && itemSpawnPoints.Length > 0)
            {
                int spawnIndex = 0;

                //전 스테이지에서 원래 바닥에 널브러져 있던 템 소환
                if (data.shipItems != null)
                {
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

                //이어하기로 들어왔을 때만 유저들 주머니에 있던 템까지 바닥에 다 소환
                if (PlayerPrefs.GetInt("SpawnInventoryOnGround", 0) == 1)
                {
                    if (data.savedInventoryItems != null)
                    {
                        foreach (int invItemID in data.savedInventoryItems)
                        {
                            ItemData itemData = ItemDatabase.Instance.GetItem(invItemID);
                            if (itemData != null && itemData.itemPrefab != null)
                            {
                                NetworkObject prefabNetObj = itemData.itemPrefab.GetComponent<NetworkObject>();
                                Transform currentPoint = itemSpawnPoints[spawnIndex % itemSpawnPoints.Length];
                                Runner.Spawn(prefabNetObj, currentPoint.position, currentPoint.rotation);
                                spawnIndex++;
                            }
                        }
                    }
                    PlayerPrefs.SetInt("SpawnInventoryOnGround", 0);
                    PlayerPrefs.Save();
                }
            }
        }
        else
        {
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
                newData.roomName = slotData.roomName;
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