using Fusion;
using UnityEngine;

public class AntidoteControl : InvenBase
{
    public override ToolType Type => ToolType.Antidote;

    [Header("Antidote")]
    public float distance = 5f;
    public float aimRadius = 0.45f;
    public LayerMask targetMask = ~0;

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

            gas.AddExposure(-30f);
            inventory.ConsumeSelectedItem();
        }

        // 상대한테 사용
        if (Input.GetMouseButtonDown(1))
        {
            inventory = GetComponentInParent<PhotonInventory>();
            if (inventory == null) return;

            PlayerGas myGas = GetComponentInParent<PlayerGas>();
            if (myGas == null) return;

            PlayerGas targetGas = FindTargetPlayerGas(myGas);

            if (targetGas == null) return;

            NetworkObject targetObj = targetGas.Object;

            if (targetObj == null) return;

            myGas.RequestApplyExposureToTarget(targetObj, -30f);

            inventory.ConsumeSelectedItem();
        }
    }

    private PlayerGas FindTargetPlayerGas(PlayerGas myGas)
    {
        if (interactor == null || interactor.cam == null)
            return null;

        Vector3 origin = interactor.cam.position;
        Vector3 direction = interactor.cam.forward;

        Debug.DrawRay(origin, direction * distance, Color.red, 1f);

        Ray ray = new Ray(origin, direction);

        RaycastHit[] hits = Physics.SphereCastAll(
            ray,
            aimRadius,
            distance,
            targetMask,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0) return null;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            PlayerGas targetGas = hit.collider.GetComponentInParent<PlayerGas>();

            if (targetGas == null) continue;
            if (targetGas == myGas) continue;

            return targetGas;
        }

        return null;
    }
}