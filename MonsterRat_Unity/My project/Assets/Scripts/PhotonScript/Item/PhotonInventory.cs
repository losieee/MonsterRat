using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;

public class PhotonInventory : MonoBehaviour
{
    [Header("UI 설정")]
    public GameObject inventoryPanel;
    public List<Slot> inventorySlots;

    [Header("아이템 버리기")]
    public Transform dropPoint;

    private List<ItemData> heldItems = new List<ItemData>();
    private PhotonView photonView;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        Debug.Log($"Inventory Awake: photonView is {(photonView == null ? "NULL" : "Assigned")}. IsMine: {photonView?.IsMine}");

        if (inventoryPanel == null)
        {
            inventoryPanel = GameObject.FindWithTag("InventoryPanel");
        }
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        UpdateInventoryUI();
    }

    void Update()
    {
        if (photonView != null && photonView.IsMine)
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
        if (itemToDrop.itemPrefab == null)
        {
            return;
        }
        string prefabName = "ItemPrefabs/" + itemToDrop.itemPrefab.name;
        PhotonNetwork.Instantiate(prefabName, dropPoint.position, dropPoint.rotation);
        heldItems.RemoveAt(slotIndex);
        UpdateInventoryUI();
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
}