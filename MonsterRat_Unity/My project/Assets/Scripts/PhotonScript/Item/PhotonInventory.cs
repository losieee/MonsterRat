using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Fusion;
using System.Linq;
using System;
using System.Security; // Enum 파싱용

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

    // ToolType에 따라 보여지는 오브젝트 관리
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


        if (HasInputAuthority && PlayerDataVault.HasData(Runner.LocalPlayer))
        {
            List<int> savedIDs = PlayerDataVault.GetInventory(Runner.LocalPlayer);
            for (int i = 0; i < savedIDs.Count; i++)
            {
                if (i < heldItems.Length && savedIDs[i] != -1)
                {
                    heldItems[i] = ItemDatabase.Instance.GetItem(savedIDs[i]);
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

    // 도구 오브젝트 활성화 / 비활성화
    private void UpdateToolObjects(ToolType activeType)
    {
        for (int i = 0; i < toolObjects.Count; i++)
        {
            if (toolObjects[i] == null || toolObjects[i].obj == null)
                continue;

            // Hand면 전부 false
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

    // 토글 슬롯 방식
    // private void ToggleSlot(int slotIndex)
    // {
    //     if (slotIndex < 0 || slotIndex >= heldItems.Length) return;
    //
    //     if (heldItems[slotIndex] == null) return;
    //
    //     if (currentSelectedSlot == slotIndex)
    //     {
    //         currentSelectedSlot = -1;
    //         RPC_ChangeTool(ToolType.Hand);
    //     }
    //     else
    //     {
    //         currentSelectedSlot = slotIndex;
    //         ItemData data = heldItems[slotIndex];
    //         if (Enum.TryParse(data.itemName, true, out ToolType type))
    //         {
    //             RPC_ChangeTool(type);
    //         }
    //         else
    //         {
    //             RPC_ChangeTool(ToolType.Hand);
    //         }
    //     }
    // }

    private void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= heldItems.Length) return;
        if (heldItems[slotIndex] == null) return; // 빈 슬롯이면 무시

        currentSelectedSlot = slotIndex;
        ItemData data = heldItems[slotIndex];

        // 인벤토리 몇번을 사용하고 있는지 표시
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

    // 실제 도구 스크립트를 켜고 끄는 로직
    private void SwitchToolLogic(ToolType type)
    {
        if (currentTool != null) currentTool.OnDeselect();

        currentTool = tools.FirstOrDefault(t => t.Type == type);

        UpdateToolObjects(type);

        if (currentTool != null)
        {
            currentTool.OnSelect();
            // Debug.Log($"무기 변경됨: {type}");
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

                    // 방독면을 버리는 순간 기능 정지
                    gasMask.StopMaskEffectOnly();
                    gasMask.HideGasMaskUI();
                }
            }
            if(dropType == ToolType.Flash)
            {
                FlashController flash =
                    tools.FirstOrDefault(t => t.Type == ToolType.Flash) as FlashController;

                if(flash)
                {
                    flash.RPC_SetFlash(false);
                    Debug.Log("꺼짐");
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

        // 버린 자리는 비워듀기
        heldItems[slotIndex] = null;

        // 버린 후에 맨손 유지
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

        List<int> currentIDs = new List<int>();
        foreach (var item in heldItems)
        {
            if (item != null) currentIDs.Add(item.itemID);
            else currentIDs.Add(-1); // 빈 슬롯 표시
        }

        // 내 정보를 이용해 정적 클래스에 저장
        PlayerDataVault.SaveInventory(Runner.LocalPlayer, currentIDs);
    }

}