using UnityEngine;

public class AntidoteControl : InvenBase
{
    public override ToolType Type => ToolType.Antidote;

    public float distance = 3f;

    private PlayerGas gas;
    private PhotonInventory inventory;

    public override void Tick()
    {
        // 본인한테 사용
        if (Input.GetMouseButtonDown(0))
        {
            gas = GetComponentInParent<PlayerGas>();
            inventory = GetComponentInParent<PhotonInventory>();

            if (gas == null || inventory == null) return;

            gas.AddExposure(-30);

            inventory.ConsumeSelectedItem();
        }
        // 상대한테 사용
        if (Input.GetMouseButtonDown(1))
        {
            inventory = GetComponentInParent<PhotonInventory>();
            if (inventory == null) return;

            if (interactor == null || interactor.cam == null) return;

            Ray ray = new Ray(interactor.cam.position, interactor.cam.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                PlayerGas targetGas = hit.collider.GetComponentInParent<PlayerGas>();

                if (targetGas == null) return;

                PlayerGas myGas = GetComponentInParent<PlayerGas>();

                // 본인한테 우클릭 적용 방지
                if (targetGas == myGas) return;

                targetGas.RPC_AddExposure(-30);
                inventory.ConsumeSelectedItem();
            }
        }
    }
}
