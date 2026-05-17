using UnityEngine;
using Fusion;
using System.Linq;

[RequireComponent(typeof(PhotonInventory))]
public class PlayerInteraction : NetworkBehaviour
{
    [Header("아이템 줍기 설정 (E 키)")]
    public float pickupDistance = 3f;
    public LayerMask itemLayer;
    public KeyCode pickupKey = KeyCode.E;

    [Header("소각장 관련")]
    public float interactDistance = 2f;
    public Transform PushBtCamera;
    public LayerMask ButtonLayer;
    public KeyCode PushButton = KeyCode.F;

    private Camera playerCamera;
    private PhotonInventory inventory;
    private ItemObject currentInteractableItem;

    void Awake()
    {
        inventory = GetComponent<PhotonInventory>();
        //playerCamera = Camera.main; // 1인칭 화면을 담당하는 메인 카메라
    }

    public override void Spawned()
    {
        if (!HasInputAuthority) enabled = false;
    }

    void Update()
    {
        if (!HasInputAuthority) return;
        CheckForInteractableItems();

        if (Input.GetKeyDown(pickupKey))
        {
            if (currentInteractableItem != null)
            {
                TryPickupItem();
            }
        }

        if (Input.GetKeyDown(PushButton))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Ray ray = new Ray(PushBtCamera.position, PushBtCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, ButtonLayer))
        {
            DestroyController incineratorButton = hit.collider.GetComponent<DestroyController>();
            if (incineratorButton != null)
            {
                incineratorButton.CanDelete();
            }
        }
    }

    private void CheckForInteractableItems()
    {
        if (PushBtCamera == null) return;

        Ray ray = new Ray(PushBtCamera.position, PushBtCamera.forward);

        Debug.DrawRay(ray.origin, ray.direction * pickupDistance, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance, itemLayer))
        {
            ItemObject item = hit.collider.GetComponentInParent<ItemObject>();
            if (item != null)
            {
                currentInteractableItem = item;
                return; // 성공적으로 찾음
            }
        }
        currentInteractableItem = null;
    }

    private void TryPickupItem()
    {
        if (currentInteractableItem == null) return;

        NetworkObject itemNetObj = currentInteractableItem.GetComponent<NetworkObject>();

        if (itemNetObj == null)
        {
            return;
        }

        float gasMaskCooldown = 0f;
        bool isGasMask = false;

        if (currentInteractableItem.itemData != null)
        {
            isGasMask = currentInteractableItem.itemData.itemName.Equals(
                ToolType.GasMask.ToString(),
                System.StringComparison.OrdinalIgnoreCase
            );
        }

        if (isGasMask)
        {
            DroppedGasMaskState gasMaskState =
                currentInteractableItem.GetComponent<DroppedGasMaskState>();

            if (gasMaskState != null)
            {
                gasMaskCooldown = gasMaskState.CooldownRemaining;
            }
        }

        if (inventory.AddItem(currentInteractableItem.itemData))
        {
            if (isGasMask)
            {
                inventory.ApplyGasMaskCooldownFromPickup(gasMaskCooldown);
            }

            if (Runner.IsServer)
            {
                Runner.Despawn(itemNetObj);
            }
            else
            {
                RPC_RequestDespawnItem(itemNetObj.Id);
                currentInteractableItem.gameObject.SetActive(false);
            }
        }
        currentInteractableItem = null;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestDespawnItem(NetworkId itemId)
    {
        if (Runner.TryFindObject(itemId, out NetworkObject itemToDespawn))
        {
            Runner.Despawn(itemToDespawn);
        }
    }
}