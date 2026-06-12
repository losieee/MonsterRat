using System.Collections;
using UnityEngine;
using SlimUI.ModernMenu;

public static class GameInputLock               // 플레이어 움직임 잠금
{
    public static bool IsLocked { get; private set; }

    public static void Lock()
    {
        IsLocked = true;
    }

    public static void Unlock()
    {
        IsLocked = false;
    }
}

public class SpannerMiniGame : MonoBehaviour
{
    public GameObject miniGamePanel;
    public RectTransform barBackground;
    public RectTransform movingBox;
    public RectTransform successZone;
    public RectTransform leftPoint;
    public RectTransform rightPoint;
    public GameObject aimDot;

    public float moveSpeed = 300f;
    public int successCount = 4;

    private int currentSuccessCount = 0;
    private bool isPlaying = false;
    private bool movingRight = true;

    private GasValveSync targetValve;
    private PhotonSpanner ownerSpanner;         // 사운드 전용

    public bool IsPlaying => isPlaying;

    private void Start()
    {
        if (miniGamePanel != null)
            miniGamePanel.SetActive(false);
    }

    private void Update()
    {
        if (!isPlaying) return;

        MoveBox();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (IsBoxInsideSuccessZone())
            {
                SuccessOneStep();
            }
            else
            {
                Fail();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
        }
    }

    public void StartMiniGame(GasValveSync valve, PhotonSpanner spanner)
    {
        if (spanner == null || !spanner.HasInputAuthority)
            return;

        if (isPlaying) return;

        targetValve = valve;
        ownerSpanner = spanner;

        if (aimDot != null)
            aimDot.SetActive(false);

        isPlaying = true;
        currentSuccessCount = 0;

        // 미니게임 플레이 중 esc를 누를 시 설정말고 미니게임이 취소되도록
        if (UISettingsManager.Instance != null)
            UISettingsManager.Instance.canUseEscKey = false;

        GameInputLock.Lock();

        if (miniGamePanel != null)
            miniGamePanel.SetActive(true);

        ResetRound();
    }

    private void MoveBox()
    {
        if (movingBox == null || leftPoint == null || rightPoint == null)
            return;

        Vector2 targetPos = movingRight ? rightPoint.anchoredPosition : leftPoint.anchoredPosition;

        movingBox.anchoredPosition = Vector2.MoveTowards(movingBox.anchoredPosition, targetPos, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(movingBox.anchoredPosition, targetPos) < 1f)
        {
            movingRight = !movingRight;
        }
    }

    // MovingBox의 가운데 점이 SuccessZone 안에 들어있으면 성공 처리
    private bool IsBoxInsideSuccessZone()
    {
        if (movingBox == null || successZone == null)
            return false;

        float boxX = movingBox.anchoredPosition.x;

        float zoneLeft = successZone.anchoredPosition.x - successZone.rect.width * 0.5f;
        float zoneRight = successZone.anchoredPosition.x + successZone.rect.width * 0.5f;

        return boxX >= zoneLeft && boxX <= zoneRight;
    }

    private void SuccessOneStep()
    {
        if (ownerSpanner != null)
            ownerSpanner.Rpc_StartSpannerSound();

        currentSuccessCount++;

        if (currentSuccessCount >= successCount)
        {
            CompleteMiniGame();
            return;
        }

        ResetRound();
    }

    private void CompleteMiniGame()
    {
        if (targetValve != null)
        {
            targetValve.FixValve();
        }

        EndMiniGame();
    }

    private void Fail()
    {
        currentSuccessCount = 0;

        ResetRound();
    }

    private void ResetRound()
    {
        movingRight = true;

        if (movingBox != null && leftPoint != null)
            movingBox.anchoredPosition = leftPoint.anchoredPosition;

        RandomizeSuccessZone();
    }

    private void RandomizeSuccessZone()
    {
        if (barBackground == null || successZone == null)
            return;

        // 뒷배경의 절반 넓이
        float barHalfWidth = barBackground.rect.width * 0.5f;

        // 랜덤 생성 될 SuccessZone의 절반 넓이를 구한 다음
        float zoneHalfWidth = successZone.rect.width * 0.5f;

        // 뒷배경에서 빠져나가지 않게 최소 / 최대 값을 구하고 여유를 둠
        float minX = -barHalfWidth + zoneHalfWidth;     // 뒷배경에서 왼쪽 끝 + 절반 넓이
        float maxX = barHalfWidth - zoneHalfWidth;      // 뒷배경에서 오른쪽 끝 + 절반 넓이

        float randomX = Random.Range(minX, maxX);
        successZone.anchoredPosition = new Vector2(randomX, successZone.anchoredPosition.y);
    }

    private void Cancel()
    {
        EndMiniGame();
    }

    private void EndMiniGame()
    {
        isPlaying = false;
        GetComponent<PhotonPlayerUIState>().ApplyMainUIVisible(true);
        targetValve = null;
        ownerSpanner = null;
        currentSuccessCount = 0;

        if (UISettingsManager.Instance != null)
            UISettingsManager.Instance.canUseEscKey = true;

        GameInputLock.Unlock();

        if (miniGamePanel != null)
            miniGamePanel.SetActive(false);
    }
}