using UnityEngine;
using Fusion; // Photon.Pun 대신 Fusion 사용
using System.Linq;

[RequireComponent(typeof(PhotonInventory))]
public class PlayerInteraction : NetworkBehaviour
{
    [Header("아이템 줍기 설정 (E 키)")]
    public float pickupRadius = 2f;
    public LayerMask itemLayer;
    public KeyCode pickupKey = KeyCode.E;

    private Camera playerCamera;
    private PhotonInventory inventory;
    private ItemObject currentInteractableItem;

    void Awake()
    {
        inventory = GetComponent<PhotonInventory>();
        if (inventory == null)
        {
            Debug.LogError("PlayerInteraction: Inventory 스크립트를 찾을 수 없습니다!", this.gameObject);
        }
        playerCamera = Camera.main;
    }
    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (!HasInputAuthority) return;

        CheckForInteractableItems(); 

        if (currentInteractableItem != null && Input.GetKeyDown(pickupKey))  
        {
            TryPickupItem();
        }
        if (inventory == null) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)) { inventory.DropItem(0); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { inventory.DropItem(1); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { inventory.DropItem(2); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { inventory.DropItem(3); }
        // if (Input.GetKeyDown(KeyCode.Alpha5)) { inventory.DropItem(4); }
        // if (Input.GetKeyDown(KeyCode.Alpha6)) { inventory.DropItem(5); }
    }

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

        NetworkObject itemNetObj = currentInteractableItem.GetComponent<NetworkObject>();

        inventory.AddItem(currentInteractableItem.itemData);
        if (itemNetObj != null)
        {
            if (Runner.IsServer)
            {
                Runner.Despawn(itemNetObj);
            }
            else
            {
                RPC_RequestDespawnItem(itemNetObj);
            }
        }
        else
        {
            Destroy(currentInteractableItem.gameObject);
        }

        currentInteractableItem = null;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestDespawnItem(NetworkObject itemToDespawn)
    {
        if (itemToDespawn != null)
        {
            Runner.Despawn(itemToDespawn);
        }
    }
}