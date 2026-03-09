using UnityEngine;

public class TutorialStoreUI : MonoBehaviour
{
    public TutorialInventory inventory;
    public ToolSpawnner spawnner;
    public Transform toolSpawn;

    public void BuyGun()
    {
        spawnner.SpawnTool(toolSpawn, 1);
        inventory.clickGun = true;
        //if (inventory == null) return;

        // 이미 총을 갖고 있으면 아무 일도 안 일어남
        //inventory.AddTool(TutorialToolType.Gun);
        //inventory.hasGun = true;
    }

    public void BuyMop()
    {
        spawnner.SpawnTool(toolSpawn, 0);
        inventory.clickMop = true;
        //if (inventory == null) return;
        //inventory.AddTool(TutorialToolType.Mop);
        //inventory.hasMop = true;
    }

    public void BuySpanner()
    {
        spawnner.SpawnTool(toolSpawn, 2);
        inventory.clickSpanner = true;
        //if (inventory == null) return;
        //inventory.AddTool(TutorialToolType.Spanner);
        //inventory.hasSpanner = true;
    }
}
