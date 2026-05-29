using UnityEngine;

public class AntidoteControl : InvenBase
{
    public override ToolType Type => ToolType.Antidote;

    private PlayerGas gas;
    private PhotonInventory inventory;

    public override void Tick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            gas = GetComponentInParent<PlayerGas>();
            inventory = GetComponentInParent<PhotonInventory>();

            if (gas == null || inventory == null) return;

            gas.AddExposure(-30);

            inventory.ConsumeSelectedItem();
        }
    }
}
