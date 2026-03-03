using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public Inventory inventory;
    public TutorialDialogueSystem dialogue;
    public SafeZone_Door door;
    public Image clearGaugeFill;

    // 튜토리얼 진행 (청소 진행도 100% 달성시 처리할것들)
    private bool tutorial1Clear = false;    // 물건 구매 완료
    private bool tutorial2Clear = false;    // 쓰레기 처리 완료
    private bool tutorial3Clear = false;    // 얼룩 지우기 완료
    private bool tutorial4Clear = false;    // 몬스터 처치 완료
    private bool tutorial5Clear = false;    // 배관 수리 완료

    private bool reached60Once = false;
    private float case3StartTime = -999f;
    private bool case3Started = false;
    // 쥐 사살 카운트
    private int tutorial3AliveRats = 0;
    private bool waitingRatsClear = false;

    // 쓰레기 처리부터 시작
    public int gaugeStep = 2;

    private bool wasFull = false;

    // 몬스터 랜덤 스폰 좌표
    public Transform[] ratSpawner;
    public GameObject ratPreb;

    // 가스 연출
    public GameObject gasAction1;
    public GameObject gasAction2;
    public GameObject spannerOutLine;
    public GameObject barrier;

    // 가이드라인
    public GameObject guaid1;
    public GameObject guaid2;
    public GameObject guaid3;

    void Update()
    {
        CheckCleaningTool();
        CheckGauge60Event();
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
                    guaid1.SetActive(false);
                    guaid2.SetActive(true);
                    inventory.UnlockTool(ToolType.Mop);
                    dialogue.ResumeAuto();
                    StartCoroutine(Tutorial2Tmi());
                }
                break;

            case 3:     // 얼룩
                if (!tutorial3Clear)
                {
                    tutorial3Clear = true;
                    case3Started = false;
                    dialogue.ResumeAuto();
                }
                break;

            case 4:     // 몬스터
                if (!tutorial4Clear)
                {
                    dialogue.ResumeAuto();
                    StartCase4();
                }
                break;

            case 5:     // 배관
                if (!tutorial5Clear)
                {
                    tutorial5Clear = true;
                    guaid3.SetActive(false);
                    dialogue.ResumeAuto();
                    StartCoroutine(Tutorial5Tmi());
                }
                break;
        }
        gaugeStep++;

        if (gaugeStep == 3)
        {
            reached60Once = false;
            case3StartTime = Time.time;
        }
    }

    void CheckGauge60Event()
    {
        if (clearGaugeFill == null) return;

        // 얼룩 지우는 단계일 때만
        if (gaugeStep != 3) return;
        if (!case3Started) return;
        if (reached60Once) return;

        if (Time.time - case3StartTime < 1.0f) return;

        // 60% 도달하면 한 번만 실행
        if (clearGaugeFill.fillAmount >= 0.6f)
        {
            reached60Once = true;

            dialogue.ResumeAuto();
            guaid2.SetActive(false);
            inventory.UnlockTool(ToolType.Gun);
            StartCoroutine(Tutorial3Tmi());
        }
    }

    // 도구 구매
    IEnumerator Tutorial1Tmi()
    {
        yield return new WaitForSeconds(3);
        door.canWork = true;
    }

    // 쓰레기 처리
    IEnumerator Tutorial2Tmi()
    {
        yield return new WaitForSeconds(5);
        GetComponent<PollutionSpawner>().PollutionSpawnOnce();

        case3Started = true;
        reached60Once = false;
        case3StartTime = Time.time;
    }

    // 얼룩 제거
    IEnumerator Tutorial3Tmi()
    {
        yield return new WaitForSeconds(2);

        tutorial3AliveRats = 4;
        waitingRatsClear = true;

        for (int i = 0; i < 4; i++)
        {
            int spawn = Random.Range(0, ratSpawner.Length);
            Instantiate(ratPreb, ratSpawner[spawn].position, ratSpawner[spawn].rotation);
        }
    }

    public void NotifyRatKilled(GameObject ratObj)
    {
        if (!waitingRatsClear) return;

        tutorial3AliveRats = Mathf.Max(0, tutorial3AliveRats - 1);

        if (tutorial3AliveRats == 0)
        {
            waitingRatsClear = false;
            tutorial3Clear = true;
            case3Started = false;
            gaugeStep = 4;
            dialogue.ResumeAuto();
        }
    }

    void StartCase4()
    {
        if (tutorial4Clear) return;
        tutorial4Clear = true;

        guaid3.SetActive(true);
        inventory.UnlockTool(ToolType.Spanner);

        StartCoroutine(Tutorial4Tmi());
    }

    // 몬스터 처치
    IEnumerator Tutorial4Tmi()
    {
        yield return new WaitForSeconds(3);
        barrier.SetActive(false);
    }

    // 배관 수리
    IEnumerator Tutorial5Tmi()
    {
        yield return null;
        spannerOutLine.SetActive(false);
        gasAction1.SetActive(false);
        gasAction2.SetActive(false);
    }
}
