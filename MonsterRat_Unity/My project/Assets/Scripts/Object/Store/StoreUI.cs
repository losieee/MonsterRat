using UnityEngine;

public class StoreUI : MonoBehaviour
{
    public Inventory inventory;

    public void BuyGun()
    {
        if (inventory == null) return;

        // 이미 총을 갖고 있으면 아무 일도 안 일어남
        inventory.AddTool(ToolType.Gun);
        inventory.hasGun = true;
    }

    public void BuyMop()
    {
        if (inventory == null) return;
        inventory.AddTool(ToolType.Mop);
        inventory.hasMop = true;
    }

    public void BuyDeGassing()
    {
        if (inventory == null) return;
        inventory.AddTool(ToolType.DeGassing);
    }

    public void BuySpanner()
    {
        if (inventory == null) return;
        inventory.AddTool(ToolType.Spanner);
        inventory.hasSpanner = true;
    }

    public void BuyCutter()
    {
        if (inventory == null) return;
        inventory.AddTool(ToolType.Cutter);
    }
}
