using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Fusion; // Photon.Pun 대신 Fusion 사용
using System.Linq;

// MonoBehaviour 대신 NetworkBehaviour 상속
public class PhotonInventory : NetworkBehaviour
{
    [Header("UI 설정")]
    public GameObject inventoryPanel;
    public List<Slot> inventorySlots;

    [Header("씬 및 버리기 설정")]
    public Transform dropPoint;
    public string lobbySceneName = "GameRoomScene"; // 대기룸 씬 이름

    private List<ItemData> heldItems = new List<ItemData>();

    // Awake 대신 Spawned()에서 초기화 (네트워크 오브젝트 권한 획득 후 실행)
    public override void Spawned()
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = GameObject.FindWithTag("InventoryPanel");
        }
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        UpdateInventoryUI();
    }

    void Update()
    {
        // photonView.IsMine 대신 HasInputAuthority 사용
        if (HasInputAuthority)
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                if (inventoryPanel != null) { inventoryPanel.SetActive(!inventoryPanel.activeSelf); }
            }
        }
    }

    public void AddItem(ItemData itemData)
    {
        if (heldItems.Count >= inventorySlots.Count)
        {
            Debug.Log("인벤토리가 꽉 찼습니다.");
            return;
        }
        heldItems.Add(itemData);
        UpdateInventoryUI();
    }

    public void DropItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= heldItems.Count) return;

        ItemData itemToDrop = heldItems[slotIndex];
        if (itemToDrop.itemPrefab == null) return;

        // PUN2에서 사용하던 프리팹 경로 유지
        string prefabPath = "ItemPrefabs/" + itemToDrop.itemPrefab.name;

        // 1. 방장(서버)이라면 직접 아이템을 스폰합니다.
        if (Runner.IsServer)
        {
            SpawnDroppedItem(prefabPath, dropPoint.position, dropPoint.rotation);
        }
        // 2. 게스트(클라이언트)라면 방장에게 떨어뜨려 달라고 RPC를 보냅니다.
        else
        {
            RPC_RequestDropItem(prefabPath, dropPoint.position, dropPoint.rotation);
        }

        heldItems.RemoveAt(slotIndex);
        UpdateInventoryUI();
    }

    // 클라이언트가 방장에게 아이템 스폰을 요청하는 RPC
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestDropItem(string prefabPath, Vector3 pos, Quaternion rot)
    {
        SpawnDroppedItem(prefabPath, pos, rot);
    }

    // 아이템을 실제로 맵에 생성하는 함수 (방장만 실행함)
    private void SpawnDroppedItem(string prefabPath, Vector3 pos, Quaternion rot)
    {
        // Resources 폴더에서 프리팹을 찾아옵니다.
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab != null)
        {
            NetworkObject netObj = prefab.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                // Fusion 전용 스폰 명령어
                Runner.Spawn(netObj, pos, rot);
            }
            else
            {
                Debug.LogError($"[Fusion 오류] {prefabPath} 프리팹에 'NetworkObject' 컴포넌트가 없습니다!");
            }
        }
        else
        {
            Debug.LogError($"[오류] Resources 폴더에서 {prefabPath}를 찾을 수 없습니다.");
        }
    }

    private void UpdateInventoryUI()
    {
        if (inventorySlots == null || inventorySlots.Count == 0) return;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i] == null) continue;
            if (i < heldItems.Count && heldItems[i] != null)
            {
                inventorySlots[i].DrawSlot(heldItems[i]);
            }
            else
            {
                inventorySlots[i].ClearSlot();
            }
        }
    }

    public bool HasItem(string itemID)
    {
        return heldItems.Any(item => item.itemName == itemID);
    }

    public void RemoveItem(string itemID)
    {
        ItemData itemToRemove = heldItems.FirstOrDefault(item => item.itemName == itemID);
        if (itemToRemove != null)
        {
            heldItems.Remove(itemToRemove);
            UpdateInventoryUI();
        }
    }

    // --- [이전에 만들었던 '퇴장 시 아이템 처리' 로직도 퓨전에 맞게 수정 완료] ---
    void OnApplicationQuit()
    {
        HandlePlayerLeave();
    }

    public void HandlePlayerLeave()
    {
        if (!HasInputAuthority) return;

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (currentScene == lobbySceneName)
        {
            for (int i = heldItems.Count - 1; i >= 0; i--)
            {
                ItemData itemToDrop = heldItems[i];
                if (itemToDrop != null && itemToDrop.itemPrefab != null)
                {
                    string prefabPath = "ItemPrefabs/" + itemToDrop.itemPrefab.name;
                    if (Runner.IsServer)
                        SpawnDroppedItem(prefabPath, dropPoint.position, dropPoint.rotation);
                    else
                        RPC_RequestDropItem(prefabPath, dropPoint.position, dropPoint.rotation);
                }
            }
            Debug.Log("대기룸에서 퇴장: 모든 아이템을 바닥에 드롭했습니다.");
        }
        else
        {
            Debug.Log("게임씬에서 퇴장: 인벤토리 아이템이 모두 증발했습니다.");
        }

        heldItems.Clear();
        UpdateInventoryUI();
    }
}