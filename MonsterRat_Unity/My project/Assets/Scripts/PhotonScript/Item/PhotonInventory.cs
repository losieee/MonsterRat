using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Fusion;
using System.Linq;
using System; // Enum 파싱용

public class PhotonInventory : NetworkBehaviour
{
    [Header("UI 설정")]
    public GameObject inventoryPanel;
    public List<Slot> inventorySlots;

    [Header("씬 및 버리기 설정")]
    public Transform dropPoint;
    public string lobbySceneName = "GameRoomScene";

    private ItemData[] heldItems;
    private int currentSelectedSlot = -1;  

    [Header("도구 관리 InvenBase 연동")]
    private InvenBase[] tools;
    private InvenBase currentTool;

    [Networked]
    public ToolType NetActiveTool { get; set; }
    private ToolType _localActiveTool = (ToolType)(-1);

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

        if (Runner.IsServer) SpawnDroppedItem(prefabPath, dropPoint.position, dropPoint.rotation);
        else RPC_RequestDropItem(prefabPath, dropPoint.position, dropPoint.rotation);

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
    public void RPC_RequestDropItem(string prefabPath, Vector3 pos, Quaternion rot)
    {
        SpawnDroppedItem(prefabPath, pos, rot);
    }

    private void SpawnDroppedItem(string prefabPath, Vector3 pos, Quaternion rot)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab != null)
        {
            NetworkObject netObj = prefab.GetComponent<NetworkObject>();
            if (netObj != null) Runner.Spawn(netObj, pos, rot);
        }
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
}