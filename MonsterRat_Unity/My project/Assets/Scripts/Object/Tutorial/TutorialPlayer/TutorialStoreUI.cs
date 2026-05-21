using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialStoreUI : MonoBehaviour
{
    public TutorialInventory inventory;
    public ToolSpawnner spawnner;
    public Transform toolSpawn;
    public PlayerUIState uiState;
    public Image selectIMG;
    public TMP_Text selectName;
    public TMP_Text selectInfo;
    public Sprite[] tools;

    public void SelectMop()
    {
        selectIMG.sprite = tools[0];
        selectName.text = "청소 솔";
        selectInfo.text = "얼룩 제거용 청소 도구,  가장 기본적인 청소 도구이다. \n- 얼룩 제거 속도 : 보통 \n- 기본 지급 장비";
    }

    public void SelectGun()
    {
        selectIMG.sprite = tools[1];
        selectName.text = "기본 권총";
        selectInfo.text = "비상 상황 대응용 표준 권총 \n- 장탄 수 : 8발 ";
    }

    public void SelectSpanner()
    {
        selectIMG.sprite = tools[2];
        selectName.text = "파이프렌치";
        selectInfo.text = "가스가 새어나오는 배관을 수리 할 수 있는 수리 장비이다.";
    }

    public void BuyGun()
    {
        spawnner.SpawnTool(toolSpawn, 1);
        inventory.clickGun = true;
        uiState.UseCoin(100);
        //if (inventory == null) return;

        // 이미 총을 갖고 있으면 아무 일도 안 일어남
        //inventory.AddTool(TutorialToolType.Gun);
        //inventory.hasGun = true;
    }

    public void BuyMop()
    {
        spawnner.SpawnTool(toolSpawn, 0);
        inventory.clickMop = true;
        uiState.UseCoin(100);
        //if (inventory == null) return;
        //inventory.AddTool(TutorialToolType.Mop);
        //inventory.hasMop = true;
    }

    public void BuySpanner()
    {
        spawnner.SpawnTool(toolSpawn, 2);
        inventory.clickSpanner = true;
        uiState.UseCoin(100);
        //if (inventory == null) return;
        //inventory.AddTool(TutorialToolType.Spanner);
        //inventory.hasSpanner = true;
    }
}
