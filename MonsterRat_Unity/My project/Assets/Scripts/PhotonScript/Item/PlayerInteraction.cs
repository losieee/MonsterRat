using UnityEngine;
using Photon.Pun;
using System.Linq;

 
[RequireComponent(typeof(PhotonInventory))]  
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
       // if (Input.GetKeyDown(KeyCode.Alpha5)) { inventory.DropItem(4); }
       // if (Input.GetKeyDown(KeyCode.Alpha6)) { inventory.DropItem(5); }
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

    // 여기 부분 복잡해서 AI에게 도움을 받았습니다
    private void TryPickupItem()
    {
        if (currentInteractableItem == null) return;

        PhotonView itemPhotonView = currentInteractableItem.GetComponent<PhotonView>();

        // 인벤토리에 아이템 추가
        inventory.AddItem(currentInteractableItem.itemData);

        // 네트워크 상에서 아이템 오브젝트 삭제
        if (itemPhotonView != null && itemPhotonView.ViewID != 0)
        {
            // 내가 이 아이템의 주인이면 (또는 주인이 나가서 방장인 내가 소유권을 넘겨받았다면) 직접 삭제
            if (itemPhotonView.IsMine)
            {
                PhotonNetwork.Destroy(currentInteractableItem.gameObject);
            }
            // 내가 주인이 아니면, 방장이 아니라 '해당 아이템의 진짜 주인'에게 지워달라고 부탁
            else
            {
                photonView.RPC("RPC_RequestDestroyItem", itemPhotonView.Owner, itemPhotonView.ViewID);
            }
        }
        else
        {
            // 에디터 임시 큐브 (ViewID가 0인 경우)
            Destroy(currentInteractableItem.gameObject);
        }

        currentInteractableItem = null;
    }

    [PunRPC]
    void RPC_RequestDestroyItem(int itemPhotonViewID)
    {
        PhotonView itemPV = PhotonView.Find(itemPhotonViewID);

       
        if (itemPV != null && itemPV.IsMine)
        {
            PhotonNetwork.Destroy(itemPV.gameObject);
        }
    }


}