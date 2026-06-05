using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

public class SafeZoneTrigger : NetworkBehaviour
{
    [Header("스테이지 및 세이브 설정")]
    public string currentStageName = "1Stage";
    public string lobbySceneName = "2Stage";
    public bool canCreateSaveFile = true;

    [Header("함선(스폰) 보관 구역 설정")]
    public Transform shipCenter;
    public Collider shipStorageArea;
    public LayerMask itemLayer;
    private float scanTimer = 0f;
    private int lastItemCount = -1;

    public float timeToEvacuate = 3.0f;

    [Header("UI 및 클리어 조건")]
    public GameObject interactionUI;
    public Image progressCircle;
    public TextMeshProUGUI infoText;
    public LayerMask checkLayers;

    private bool isPlayerInZone = false;
    private float currentTimer = 0f;
    private bool hasSaved = false;

    // 통신으로 받은 아이템을 임시로 모아둘 리스트
    private List<int> tempReportedItems = new List<int>();

    [Networked] public int PlayersInZoneCount { get; set; }
    [Networked] public NetworkBool IsDoorOpened { get; set; }
    [Networked] public NetworkBool IsEvacuating { get; set; }

    public void OpenSafeZone()
    {
        if (HasStateAuthority) IsDoorOpened = true;
    }

    void Start()
    {
        if (interactionUI != null) interactionUI.SetActive(false);
    }

    void Update()
    {
        if (Object == null || !Object.IsValid) return;

        if (IsEvacuating)
        {
            if (!hasSaved)
            {
                hasSaved = true;
                //SaveMyInventoryLocally();
            }
            if (infoText != null) infoText.text = "이동 중...";
            return;
        }
        if (HasStateAuthority)
        {
            scanTimer += Time.deltaTime;
            if (scanTimer >= 0.5f)
            {
                scanTimer = 0f;
                CheckRealTimeItems();
            }
        }

        if (!isPlayerInZone || !IsDoorOpened) return;

        int requiredPlayerCount = GetCurrentRoomPlayerCount();
        if (PlayersInZoneCount < requiredPlayerCount)
        {
            if (infoText != null) infoText.text = $"다른 플레이어 대기 중... ({PlayersInZoneCount}/{requiredPlayerCount})";
            return;
        }

        if (infoText != null) infoText.text = "E 키를 길게 눌러 탈출하세요";

        if (Input.GetKey(KeyCode.E))
        {
            currentTimer += Time.deltaTime;
            if (progressCircle != null) progressCircle.fillAmount = currentTimer / timeToEvacuate;

            if (currentTimer >= timeToEvacuate)
            {
                EvacuateToLobby();
            }
        }
        else
        {
            currentTimer = 0f;
            if (progressCircle != null) progressCircle.fillAmount = 0f;
        }
    }

    void EvacuateToLobby()
    {
        if (IsEvacuating) return;
        //SaveMyInventoryLocally();
        hasSaved = true;
        RPC_RequestEvacuation();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestEvacuation(RpcInfo info = default)
    {
        if (IsEvacuating) return;

        ClearManager.Instance.ResetProgress();
        int requiredPlayerCount = GetCurrentRoomPlayerCount();
        if (PlayersInZoneCount >= requiredPlayerCount)
        {
            IsEvacuating = true;
            StartEvacuation();
        }
    }

    async void StartEvacuation()
    {
        if (!HasStateAuthority) return;

        bool hasLeftover = CheckForPollution();
        tempReportedItems.Clear();

        PhotonInventory[] allInvens = FindObjectsOfType<PhotonInventory>();
        foreach (var inv in allInvens)
        {
            inv.RPC_CommandSaveLocal();
            inv.RPC_CommandReportInventory(); // 권한이 있는 곳으로 직접 통신!
        }
        await System.Threading.Tasks.Task.Delay(1500);
        SaveWorldState(hasLeftover);

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Resources/Scenes/Woong/{lobbySceneName}.unity");
        if (sceneIndex >= 0)
        {
            Runner.LoadScene(SceneRef.FromIndex(sceneIndex));
        }
    }

    // 클라이언트의 PhotonInventory에서 제출한 텍스트를 장부에 적는 함수 //근데 안돼 왜 안돼 ㅇ오0애애ㅐㅗㅇ애애애
    public void AddReportedItems(string itemString)
    {
        if (!string.IsNullOrEmpty(itemString))
        {
            string[] split = itemString.Split(',');
            foreach (string s in split)
            {
                if (int.TryParse(s, out int id) && id != -1) 
                {
                    tempReportedItems.Add(id);
                }
            }
        }
    }

    private void SaveWorldState(bool hasLeftover)
    {
        WorldSaveData worldData = new WorldSaveData();
        worldData.savedStageName = lobbySceneName;
        worldData.isDoorActive = hasLeftover;

        if (PlayTimeManager.Instance != null)
        {
            worldData.playTime = PlayTimeManager.Instance.PlayTime;
        }

        if (WorldLoadManager.instance != null)
        {
            worldData.currentGold = WorldLoadManager.instance.SharedGold;
        }

        int activeSlot = PlayerPrefs.GetInt("CurrentActiveSaveSlot", 0);
        string saveKey = "SaveSlot_" + activeSlot;
        string existingJson = PlayerPrefs.GetString(saveKey, "");
        if (!string.IsNullOrEmpty(existingJson))
        {
            WorldSaveData existingData = JsonUtility.FromJson<WorldSaveData>(existingJson);
            worldData.roomName = existingData.roomName;
        }

        BoxCollider boxCol = shipStorageArea as BoxCollider;
        if (boxCol != null && shipCenter != null)
        {
            Vector3 exactCenter = boxCol.transform.TransformPoint(boxCol.center);
            Vector3 halfExtents = Vector3.Scale(boxCol.size, boxCol.transform.lossyScale) * 0.5f;

            Collider[] itemsInShip = Physics.OverlapBox(exactCenter, halfExtents, boxCol.transform.rotation, itemLayer);

            foreach (var hit in itemsInShip)
            {
                ItemObject itemObj = hit.GetComponent<ItemObject>();
                if (itemObj != null && itemObj.itemData != null)
                {
                    ShipItemData sItem = new ShipItemData();
                    sItem.itemID = itemObj.itemData.itemID;

                    sItem.localPosition = shipCenter.InverseTransformPoint(itemObj.transform.position);
                    sItem.localRotation = Quaternion.Inverse(shipCenter.rotation) * itemObj.transform.rotation;

                    worldData.shipItems.Add(sItem);
                }
            }
        }

        // 제출받아 모아둔 아이템 목록을 장부에 최종 추가
        worldData.savedInventoryItems.AddRange(tempReportedItems);

        string json = JsonUtility.ToJson(worldData);

        PlayerPrefs.SetInt("IsPollutionLeft", hasLeftover ? 1 : 0);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.SetString("MasterWorldSave", json);
        PlayerPrefs.Save();

        Debug.Log($"[WorldSave] 저장 완료! 장부에 적힌 유저들의 총 아이템 개수: {worldData.savedInventoryItems.Count}개");
    }

    private void CheckRealTimeItems()
    {
        BoxCollider boxCol = shipStorageArea as BoxCollider;
        if (boxCol == null || shipCenter == null) return;

        Vector3 exactCenter = boxCol.transform.TransformPoint(boxCol.center);
        Vector3 halfExtents = Vector3.Scale(boxCol.size, boxCol.transform.lossyScale) * 0.5f;

        Collider[] itemsInShip = Physics.OverlapBox(exactCenter, halfExtents, boxCol.transform.rotation, itemLayer);

        int currentItemCount = 0;
        foreach (var hit in itemsInShip)
        {
            if (hit.GetComponent<ItemObject>() != null)
            {
                currentItemCount++;
            }
        }

        if (lastItemCount != currentItemCount)
        {

            if (lastItemCount != -1)
            {
                if (currentItemCount > lastItemCount)
                    Debug.Log($"Safezone에 아이템 들어옴 (현재: {currentItemCount}개 보존 예정)");
                else
                    Debug.Log($"Safezone에 아이템 없어짐 (현재: {currentItemCount}개 보존 예정)");
            }
            lastItemCount = currentItemCount;
        }
    }

    private void SaveMyInventoryLocally()
    {
        PhotonInventory inv = GetComponent<PhotonInventory>();
        if (inv == null) inv = FindObjectsOfType<PhotonInventory>().FirstOrDefault(x => x.HasInputAuthority);

        if (inv != null) inv.SaveInventoryData();
    }

    private bool CheckForPollution()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.activeInHierarchy && ((1 << obj.layer) & checkLayers) != 0) return true;
        }
        return false;
    }

    private int GetCurrentRoomPlayerCount()
    {
        if (Runner == null) return 1;
        int count = 0;
        foreach (var player in Runner.ActivePlayers) count++;
        return Mathf.Max(1, count);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || !Object.IsValid || !HasStateAuthority) return;
        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                PlayersInZoneCount++;
                RPC_SetZoneUI(netObj.InputAuthority, true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (Object == null || !Object.IsValid || !HasStateAuthority) return;
        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                PlayersInZoneCount--;
                RPC_SetZoneUI(netObj.InputAuthority, false);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetZoneUI(PlayerRef targetPlayer, NetworkBool isInside)
    {
        if (Runner.LocalPlayer == targetPlayer)
        {
            isPlayerInZone = isInside;
            if (interactionUI != null) interactionUI.SetActive(isInside);
        }
    }
}