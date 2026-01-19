using UnityEngine;

public class StoreUI : MonoBehaviour
{
    public Inventory inventory;

    public void BuyGun()
    {
        if (inventory == null) return;

        // 이미 총을 갖고 있으면 아무 일도 안 일어남
        inventory.AddTool(ToolType.Gun);
    }

    public void BuyMop()
    {
        if (inventory == null) return;
        inventory.AddTool(ToolType.Mop);
    }

    public void BuyDeGassing()
    {
        if (inventory == null) return;
        inventory.AddTool(ToolType.DeGassing);
    }
}
