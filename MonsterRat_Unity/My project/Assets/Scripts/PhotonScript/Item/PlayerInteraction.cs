using UnityEngine;
using Photon.Pun;
using System.Linq;

 
[RequireComponent(typeof(Inventory))]  
public class PlayerInteraction : MonoBehaviour
{
    [Header("아이템 줍기 설정 (E 키)")]  
    public float pickupRadius = 2f;
    public LayerMask itemLayer;
    public KeyCode pickupKey = KeyCode.E; 

    private Camera playerCamera; 
    private PhotonInventory inventory;
    private PhotonView photonView;
    private ItemObject currentInteractableItem;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        inventory = GetComponent<PhotonInventory>();
        if (inventory == null)
        {
            Debug.LogError("PlayerInteraction: Inventory 스크립트를 찾을 수 없습니다!", this.gameObject);
        }

        
        playerCamera = Camera.main;
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // --- 1. 아이템 줍기 (기존 E키 로직) ---
        CheckForInteractableItems(); // E키로 주울 아이템이 있는지 확인

        if (currentInteractableItem != null && Input.GetKeyDown(pickupKey)) // 'pickupKey' (E)
        {
            TryPickupItem();
        }

        

        // --- 3. 아이템 버리기 (기존 1~6키 로직) ---
        if (inventory == null) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)) { inventory.DropItem(0); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { inventory.DropItem(1); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { inventory.DropItem(2); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { inventory.DropItem(3); }
        if (Input.GetKeyDown(KeyCode.Alpha5)) { inventory.DropItem(4); }
        if (Input.GetKeyDown(KeyCode.Alpha6)) { inventory.DropItem(5); }
    }

    // --- [기존 아이템 줍기 함수들] ---
    private void CheckForInteractableItems()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRadius, itemLayer);

        ItemObject closestItem = null;
        float closestDistance = float.MaxValue;

        if (hitColliders.Length > 0)
        {
            foreach (var hit in hitColliders)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    ItemObject item = hit.GetComponent<ItemObject>();
                    if (item != null)
                    {
                        closestDistance = distance;
                        closestItem = item;
                    }
                }
            }
        }
        currentInteractableItem = closestItem;
    }

    private void TryPickupItem()
    {
        if (currentInteractableItem == null) return;

        PhotonView itemPhotonView = currentInteractableItem.GetComponent<PhotonView>();

        inventory.AddItem(currentInteractableItem.itemData);

        //  ViewID가 0이 아닐 때만 포톤으로 삭제
        if (itemPhotonView != null && itemPhotonView.ViewID != 0)
        {
            if (itemPhotonView.IsMine || PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(currentInteractableItem.gameObject);
            }
            else
            {
                photonView.RPC("RPC_RequestDestroyItem", RpcTarget.MasterClient, itemPhotonView.ViewID);
            }
        }
        else
        {
            // ViewID가 0인 씬 큐브 PhotonView가 없는 오브젝트는 기본 Destroy 사용
            Destroy(currentInteractableItem.gameObject);
        }

        currentInteractableItem = null;
    }

    [PunRPC]
    void RPC_RequestDestroyItem(int itemPhotonViewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView itemPV = PhotonView.Find(itemPhotonViewID);
        if (itemPV != null)
        {
            PhotonNetwork.Destroy(itemPV.gameObject);
        }
    }

     
}