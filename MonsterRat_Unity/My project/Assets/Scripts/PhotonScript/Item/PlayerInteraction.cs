using UnityEngine;
using Fusion;
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
        playerCamera = Camera.main;
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
            Debug.Log($"?? [줍기 시도] E키 눌림! 현재 내 레이어 마스크 세팅값: {itemLayer.value}");

            if (currentInteractableItem != null)
            {
                Debug.Log($"?? [줍기 성공 직전] 눈앞에 {currentInteractableItem.name} 발견! 주머니에 넣습니다.");
                TryPickupItem();
            }
            else
            {
                Debug.LogWarning("? [줍기 실패] 눈앞에 아이템이 안 보입니다! (LayerMask나 아이템의 Layer 설정 문제)");
            }
        }
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

        if (inventory.AddItem(currentInteractableItem.itemData))
        {
            if (itemNetObj != null)
            {
                if (Runner.IsServer)
                {
                    Debug.Log($"?? [방장 권한] 방장이 아이템({itemNetObj.Id})을 직접 삭제합니다.");
                    Runner.Despawn(itemNetObj);
                }
                else
                {
                    Debug.Log($"?? [게스트 권한] 게스트가 아이템({itemNetObj.Id})을 먹었습니다. 방장에게 삭제를 요청합니다!");
                    RPC_RequestDespawnItem(itemNetObj.Id);
                }
            }
            else
            {
                Destroy(currentInteractableItem.gameObject);
            }
        }
        currentInteractableItem = null;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestDespawnItem(NetworkId itemId)
    {
        Debug.Log($"?? [RPC 수신] 방장이 게스트의 아이템({itemId}) 삭제 요청을 받았습니다!");
        if (Runner.TryFindObject(itemId, out NetworkObject itemToDespawn))
        {
            Runner.Despawn(itemToDespawn);
            Debug.Log($"? [RPC 처리 완료] 방장이 게스트의 부탁을 받고 아이템을 세상에서 지웠습니다.");
        }
        else
        {
            Debug.LogError($"?? [RPC 에러] 방장이 {itemId} 번호표를 가진 아이템을 찾지 못했습니다! (이미 삭제됐거나 동기화 오류)");
        }
    }
}