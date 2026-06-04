using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Fusion;
using System.Linq;
using System;
using System.Security;

//인벤토리의 아이템 ID들을 Json으로 변환시킬 클래스 
[System.Serializable]
public class InventorySaveData
{
    public List<int> itemIDs = new List<int>();
}

public class PhotonInventory : NetworkBehaviour
{
    [System.Serializable]
    public class ToolVisualEntry
    {
        public ToolType type;
        public GameObject obj;
    }

    [Header("UI 설정")]
    public GameObject inventoryPanel;
    public List<Slot> inventorySlots;
    public GameObject[] selectSlots;

    [Header("씬 및 버리기 설정")]
    public Transform dropPoint;
    public string lobbySceneName = "GameRoomScene";

    [Header("인벤 복구 용")]
    private ItemData[] heldItems;
    private int currentSelectedSlot = -1;

    [Header("도구 관리 InvenBase 연동")]
    private InvenBase[] tools;
    private InvenBase currentTool;

    [Networked]
    public ToolType NetActiveTool { get; set; }
    private ToolType _localActiveTool = (ToolType)(-1);

    [SerializeField] private List<ToolVisualEntry> toolObjects = new List<ToolVisualEntry>();

    public override void Spawned()
    {
        heldItems = new ItemData[inventorySlots.Count];

        if (inventoryPanel == null) inventoryPanel = GameObject.FindWithTag("InventoryPanel");
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        tools = GetComponentsInChildren<InvenBase>(true);
        PlayerUIState ui = GetComponent<PlayerUIState>();
        PlayerRaycast interactor = GetComponentInChildren<PlayerRaycast>(true);

        foreach (var t in tools)
        {
            t.Init(ui, interactor);
            t.OnDeselect();
        }

        if (HasInputAuthority) RPC_ChangeTool(ToolType.Hand);

        // 스폰될 떄 로비에서 왔는가? 또는 게임을 이어서 하는가?
        if (HasInputAuthority)
        {
            // 닉네임 대신 고유 번호표 
            string myUniqueKey = "MySavedInventory_" + Runner.LocalPlayer.ToString();

            if (PlayerPrefs.GetInt("JoinedFromLobby", 0) == 1)
            {
                PlayerPrefs.DeleteKey(myUniqueKey);
                PlayerPrefs.SetInt("JoinedFromLobby", 0);
                PlayerPrefs.Save();
            }
            else
            {
                string jsonString = PlayerPrefs.GetString(myUniqueKey, "");
                if (!string.IsNullOrEmpty(jsonString))
                {
                    InventorySaveData loadedData = JsonUtility.FromJson<InventorySaveData>(jsonString);
                    for (int i = 0; i < loadedData.itemIDs.Count; i++)
                    {
                        if (i < heldItems.Length && loadedData.itemIDs[i] != -1)
                        {
                            heldItems[i] = ItemDatabase.Instance.GetItem(loadedData.itemIDs[i]);
                        }
                    }
                    PlayerPrefs.DeleteKey(myUniqueKey);
                }
            }
        }
        UpdateInventoryUI();
    }

    void Update()
    {
        if (!HasInputAuthority) return;

        if (Input.GetKeyDown(KeyCode.I))
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            currentSelectedSlot = -1;

            for (int i = 0; i < selectSlots.Length; i++)
            {
                selectSlots[i].SetActive(false);
            }

            RPC_ChangeTool(ToolType.Hand);
        }
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);

        if (Input.GetKeyDown(KeyCode.G))
        {
            if (currentSelectedSlot != -1 && heldItems[currentSelectedSlot] != null)
            {
                DropItem(currentSelectedSlot);
            }
        }

        if (currentTool != null)
        {
            currentTool.Tick();
        }
    }

    void FixedUpdate()
    {
        if (HasInputAuthority && currentTool != null)
        {
            currentTool.FixedTick();
        }
    }

    private void UpdateToolObjects(ToolType activeType)
    {
        for (int i = 0; i < toolObjects.Count; i++)
        {
            if (toolObjects[i] == null || toolObjects[i].obj == null)
                continue;

            if (activeType == ToolType.Hand)
            {
                toolObjects[i].obj.SetActive(false);
            }
            else
            {
                toolObjects[i].obj.SetActive(toolObjects[i].type == activeType);
            }
        }
    }

    public bool AddItem(ItemData itemData)
    {
        for (int i = 0; i < heldItems.Length; i++)
        {
            if (heldItems[i] == null)
            {
                heldItems[i] = itemData;
                UpdateInventoryUI();
                return true;
            }
        }
        return false;
    }

    private void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= heldItems.Length) return;
        if (heldItems[slotIndex] == null) return;

        currentSelectedSlot = slotIndex;
        ItemData data = heldItems[slotIndex];

        for (int i = 0; i < selectSlots.Length; i++)
        {
            selectSlots[i].SetActive(false);
        }

        selectSlots[slotIndex].SetActive(true);

        if (Enum.TryParse(data.itemName, true, out ToolType type))
        {
            RPC_ChangeTool(type);
        }
        else
        {
            Debug.LogWarning($"{data.itemName}은(는) 일치하는 ToolType이 없습니다! 맨손으로 대체합니다.");
            RPC_ChangeTool(ToolType.Hand);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ChangeTool(ToolType newTool)
    {
        NetActiveTool = newTool;
    }

    public override void Render()
    {
        if (Object == null || !Object.IsValid) return;

        if (NetActiveTool != _localActiveTool)
        {
            SwitchToolLogic(NetActiveTool);
            _localActiveTool = NetActiveTool;
        }
    }

    private void SwitchToolLogic(ToolType type)
    {
        if (currentTool != null) currentTool.OnDeselect();

        currentTool = tools.FirstOrDefault(t => t.Type == type);

        UpdateToolObjects(type);

        if (currentTool != null)
        {
            currentTool.OnSelect();
        }
    }

    public void DropItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= heldItems.Length) return;

        ItemData itemToDrop = heldItems[slotIndex];
        if (itemToDrop == null || itemToDrop.itemPrefab == null) return;

        string prefabPath = "ItemPrefabs/" + itemToDrop.itemPrefab.name;

        float gasMaskCooldown = 0f;

        if (Enum.TryParse(itemToDrop.itemName, true, out ToolType dropType))
        {
            if (dropType == ToolType.GasMask)
            {
                PhotonGasMaskController gasMask =
                    tools.FirstOrDefault(t => t.Type == ToolType.GasMask) as PhotonGasMaskController;

                if (gasMask != null)
                {
                    gasMaskCooldown = gasMask.GetCooldownRemaining();
                    gasMask.StopMaskEffectOnly();
                    gasMask.HideGasMaskUI();
                }
            }
            if (dropType == ToolType.Flash)
            {
                FlashController flash =
                    tools.FirstOrDefault(t => t.Type == ToolType.Flash) as FlashController;

                if (flash)
                {
                    flash.RPC_SetFlash(false);
                }
            }
        }

        if (Runner.IsServer)
        {
            NetworkObject dropped = SpawnDroppedItem(prefabPath, dropPoint.position, dropPoint.rotation);
            ApplyGasMaskCooldownToDroppedItem(dropped, gasMaskCooldown);
        }
        else
        {
            RPC_RequestDropItem(prefabPath, dropPoint.position, dropPoint.rotation, gasMaskCooldown);
        }

        heldItems[slotIndex] = null;

        if (currentSelectedSlot == slotIndex)
        {
            currentSelectedSlot = -1;
            RPC_ChangeTool(ToolType.Hand);
        }

        UpdateInventoryUI();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestDropItem(string prefabPath, Vector3 pos, Quaternion rot, float gasMaskCooldown)
    {
        NetworkObject dropped = SpawnDroppedItem(prefabPath, pos, rot);
        ApplyGasMaskCooldownToDroppedItem(dropped, gasMaskCooldown);
    }

    private void ApplyGasMaskCooldownToDroppedItem(NetworkObject dropped, float cooldown)
    {
        if (dropped == null) return;
        if (cooldown <= 0f) return;

        DroppedGasMaskState state = dropped.GetComponent<DroppedGasMaskState>();
        if (state != null)
        {
            state.SetCooldown(cooldown);
        }
    }

    public void ApplyGasMaskCooldownFromPickup(float cooldown)
    {
        if (cooldown <= 0f) return;

        PhotonGasMaskController gasMask =
            tools.FirstOrDefault(t => t.Type == ToolType.GasMask) as PhotonGasMaskController;

        if (gasMask != null)
        {
            gasMask.ApplyCooldownRemaining(cooldown);
        }
    }

    private NetworkObject SpawnDroppedItem(string prefabPath, Vector3 pos, Quaternion rot)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab != null)
        {
            NetworkObject netObj = prefab.GetComponent<NetworkObject>();
            if (netObj != null)
                return Runner.Spawn(netObj, pos, rot);
        }

        return null;
    }

    private void UpdateInventoryUI()
    {
        if (inventorySlots == null || inventorySlots.Count == 0) return;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i] == null) continue;

            if (i < heldItems.Length && heldItems[i] != null)
            {
                inventorySlots[i].DrawSlot(heldItems[i]);
            }
            else
            {
                inventorySlots[i].ClearSlot();
            }
        }
    }

    public void HandlePlayerLeave()
    {
        if (!HasInputAuthority) return;

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (currentScene == lobbySceneName)
        {
            for (int i = 0; i < heldItems.Length; i++)
            {
                if (heldItems[i] != null) DropItem(i);
            }
        }
    }

    public void SaveInventoryData()
    {
        if (!HasInputAuthority) return;

        InventorySaveData saveData = new InventorySaveData();
        foreach (var item in heldItems)
        {
            if (item != null) saveData.itemIDs.Add(item.itemID);
            else saveData.itemIDs.Add(-1);
        }

        string jsonString = JsonUtility.ToJson(saveData);
        string myUniqueKey = "MySavedInventory_" + Runner.LocalPlayer.ToString();

        PlayerPrefs.SetString(myUniqueKey, jsonString);
        PlayerPrefs.Save();
    }

    public List<int> GetHeldItemIDs()
    {
        List<int> ids = new List<int>();
        for (int i = 0; i < heldItems.Length; i++)
        {
            if (heldItems[i] != null) ids.Add(heldItems[i].itemID);
        }
        return ids;
    }

    // 해독제 전용 (인벤에서 아이템 제거 )
    public void ConsumeSelectedItem()
    {
        if (currentSelectedSlot < 0 || currentSelectedSlot >= heldItems.Length) return;
        if (heldItems[currentSelectedSlot] == null) return;

        heldItems[currentSelectedSlot] = null;

        selectSlots[currentSelectedSlot].SetActive(false);
        currentSelectedSlot = -1;

        RPC_ChangeTool(ToolType.Hand);
        UpdateInventoryUI();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_CommandSaveLocal()
    {
        if (HasInputAuthority)
        {
            SaveInventoryData();
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_CommandReportInventory()
    {
        if (HasInputAuthority)
        {
            List<int> myItems = GetHeldItemIDs();
            string itemStr = myItems.Count > 0 ? string.Join(",", myItems) : "";
            RPC_SubmitToHost(itemStr);
        }
    }

    //클라이언트 호스트한테 아이템 인벤토리 장부 내용 보내기 함수인데 안됨 
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SubmitToHost(string itemString)
    {
        Debug.Log($"[RPC 수신 확인] 호스트가 통신을 받았습니다! 받은 아이템 내용: {itemString}");
        // 방장은 이 대답을 받아서 SafeZoneTrigger의 장부에 기록
        SafeZoneTrigger trigger = FindObjectOfType<SafeZoneTrigger>();
        if (trigger != null)
        {
            trigger.AddReportedItems(itemString);
        }
    }
}