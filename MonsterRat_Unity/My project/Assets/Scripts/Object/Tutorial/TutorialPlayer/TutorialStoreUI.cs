using UnityEngine;

public class TutorialStoreUI : MonoBehaviour
{
    public TutorialInventory inventory;

    public void BuyGun()
    {
        if (inventory == null) return;

        // 이미 총을 갖고 있으면 아무 일도 안 일어남
        inventory.AddTool(TutorialToolType.Gun);
        inventory.hasGun = true;
    }

    public void BuyMop()
    {
        if (inventory == null) return;
        inventory.AddTool(TutorialToolType.Mop);
        inventory.hasMop = true;
    }

    public void BuySpanner()
    {
        if (inventory == null) return;
        inventory.AddTool(TutorialToolType.Spanner);
        inventory.hasSpanner = true;
    }
}
