using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public Inventory inventory;
    public SafeZone_Door door;
    public Image clearGaugeFill;

    // 튜토리얼 진행 (청소 진행도 100% 달성시 처리할것들)
    private bool tutorial1Clear = false;    // 물건 구매 완료
    private bool tutorial2Clear = false;    // 쓰레기 처리 완료
    private bool tutorial3Clear = false;    // 얼룩 지우기 완료
    private bool tutorial4Clear = false;    // 몬스터 처치 완료
    private bool tutorial5Clear = false;    // 배관 수리 완료

    // 쓰레기 처리부터 시작
    private int gaugeStep = 2;

    private bool wasFull = false;

    // 몬스터 랜덤 스폰 좌표
    public Transform[] ratSpawner;
    public GameObject ratPreb;

    // 가스 연출
    public GameObject gasAction1;
    public GameObject gasAction2;
    public GameObject spannerPos;
    public GameObject barrier;

    void Update()
    {
        CheckCleaningTool();
        CheckGaugeStepClear();
    }

    void CheckCleaningTool()
    {
        if (tutorial1Clear) return;
    if (!inventory.hasMop || !inventory.hasGun || !inventory.hasSpanner) return;

    tutorial1Clear = true;
    StartCoroutine(Tutorial1Tmi());
    }

    void CheckGaugeStepClear()
    {
        bool isFull = clearGaugeFill.fillAmount >= 0.999f;

        if (isFull && !wasFull)
        {
            OnGaugeFullOnce();
        }

        wasFull = isFull;
    }

    void OnGaugeFullOnce()
    {
        switch (gaugeStep)
        {
            case 2:     // 쓰레기
                if (!tutorial2Clear)
                {
                    tutorial2Clear = true;
                    inventory.UnlockTool(ToolType.Mop);
                    StartCoroutine(Tutorial2Tmi());
                }
                break;

            case 3:     // 얼룩
                if (!tutorial3Clear)
                {
                    tutorial3Clear = true;
                    inventory.UnlockTool(ToolType.Gun);
                    StartCoroutine(Tutorial3Tmi());
                }
                break;

            case 4:     // 몬스터
                if (!tutorial4Clear)
                {
                    tutorial4Clear = true;
                    inventory.UnlockTool(ToolType.Spanner);
                    StartCoroutine(Tutorial4Tmi());
                }
                break;

            case 5:     // 배관
                if (!tutorial5Clear)
                {
                    tutorial5Clear = true;
                    StartCoroutine(Tutorial5Tmi());
                }
                break;
        }
        gaugeStep++;

        clearGaugeFill.fillAmount = 0f;
        wasFull = false;
    }

    // 도구 구매
    IEnumerator Tutorial1Tmi()
    {
        yield return new WaitForSeconds(5);
        door.canWork = true;
    }

    // 쓰레기 처리
    IEnumerator Tutorial2Tmi()
    {
        yield return new WaitForSeconds(5);
        GetComponent<PollutionSpawner>().PollutionSpawnOnce();
    }

    // 얼룩 제거
    IEnumerator Tutorial3Tmi()
    {
        yield return new WaitForSeconds(5);

        for (int i = 0; i < 4; i++)
        {
            int spawn = Random.Range(0, ratSpawner.Length);
            Instantiate(ratPreb, ratSpawner[spawn], default);
        }
    }

    // 몬스터 처치
    IEnumerator Tutorial4Tmi()
    {
        yield return new WaitForSeconds(5);

        gasAction1.SetActive(true);
        gasAction2.SetActive(true);
        spannerPos.SetActive(true);
        barrier.SetActive(false);
    }

    // 배관 수리
    IEnumerator Tutorial5Tmi()
    {
        yield return null;
        gasAction1.SetActive(false);
        gasAction2.SetActive(false);
        spannerPos.SetActive(false);
    }
}
