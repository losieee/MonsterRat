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

    [Header("소각장 관련")]
    public float interactDistance = 2f;
    public Transform PushBtCamera;
    public LayerMask ButtonLayer;
    public KeyCode PushButton = KeyCode.F;
    // 레이어는 ItemLayer 쓰십쇼

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

        if (Input.GetKeyDown(PushButton)) // 그냥 정할 수 있게끔..
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        // 화면 정중앙(카메라가 보는 방향)으로 레이저를 쏩니다.
        Ray ray = new Ray(PushBtCamera.position, PushBtCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, ButtonLayer))
        {
            // 레이저에 맞은 오브젝트에 DestroyController 스크립트가 붙어있는지 확인합니다.
            DestroyController incineratorButton = hit.collider.GetComponent<DestroyController>();

            // 스크립트를 찾았다면!
            if (incineratorButton != null)
            {
                Debug.Log("[Interact] 소각장 버튼을 눌렀습니다!");

                // 여기서 핵심 함수를 실행시켜 소각장을 가동합니다.
                incineratorButton.CanDelete();
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
                Debug.Log($"[GasMask Pickup] 드랍 방독면 쿨타임 읽음: {gasMaskCooldown}");
            }
            else
            {
                Debug.LogWarning("[GasMask Pickup] DroppedGasMaskState가 드랍 프리팹에 없음");
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