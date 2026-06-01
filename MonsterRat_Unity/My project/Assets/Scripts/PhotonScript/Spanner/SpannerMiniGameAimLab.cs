using SlimUI.ModernMenu;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpannerMiniGameAimLab : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject panel;
    public RectTransform spawnArea;
    public RectTransform pipeBrokenPrefab;

    public float spawnInterval = 3f;
    public int totalNeedCount = 10;

    public AudioSource source;
    public AudioClip breakSound;
    public AudioClip successSound;

    [Header("스폰되는 균열 크기")]
    public Vector2 horizontalSize = new Vector2(160f, 60f);     // 옆이 긴 스폰 전용
    public Vector2 verticalSize = new Vector2(60f, 160f);       // 위아래가 긴 스폰 전용

    private int successCount = 0;
    private bool isGameOver = false;
    private Coroutine spawnCoroutine;

    private readonly List<GameObject> spawnedBrokens = new List<GameObject>();

    public bool IsPlaying { get; private set; }

    private GasValveSync targetValve;
    private PhotonSpanner spanner;

    public void StartMiniGame(GasValveSync valve, PhotonSpanner photonSpanner)
    {
        if (IsPlaying) return;

        ClearMiniGame();

        if (UISettingsManager.Instance != null)
            UISettingsManager.Instance.canUseEscKey = false;

        GameInputLock.Lock();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        targetValve = valve;
        spanner = photonSpanner;

        successCount = 0;
        isGameOver = false;
        IsPlaying = true;

        panel.SetActive(true);

        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (!isGameOver && successCount < totalNeedCount)
        {
            yield return new WaitForSeconds(spawnInterval);

            RectTransform broken = Instantiate(pipeBrokenPrefab, spawnArea);
            spawnedBrokens.Add(broken.gameObject);

            broken.anchorMin = new Vector2(0.5f, 0.5f);
            broken.anchorMax = new Vector2(0.5f, 0.5f);
            broken.pivot = new Vector2(0.5f, 0.5f);

            broken.localScale = Vector3.one;
            broken.localRotation = Quaternion.identity;

            bool isHorizontal = Random.value > 0.5f;
            broken.sizeDelta = isHorizontal ? horizontalSize : verticalSize;

            Rect rect = spawnArea.rect;

            float halfW = broken.sizeDelta.x * 0.5f;
            float halfH = broken.sizeDelta.y * 0.5f;

            float randomX = Random.Range(rect.xMin + halfW, rect.xMax - halfW);
            float randomY = Random.Range(rect.yMin + halfH, rect.yMax - halfH);

            broken.anchoredPosition = new Vector2(randomX, randomY);

            PipeBroken pipeBroken = broken.GetComponent<PipeBroken>();
            pipeBroken.Init(this);

            source.volume = 1f;
            source.PlayOneShot(breakSound);
        }
    }

    public void AddCount()
    {
        if (isGameOver) return;

        successCount++;

        source.volume = 0.3f;
        source.PlayOneShot(successSound);

        if (successCount >= totalNeedCount)
        {
            if (targetValve != null)
            {
                targetValve.FixValve();
            }

            EndMiniGame();
        }
    }

    public void Missed()
    {
        if (isGameOver) return;

        ClearMiniGame();

        isGameOver = true;
        IsPlaying = false;

        panel.SetActive(false);

        if (UISettingsManager.Instance != null)
            UISettingsManager.Instance.canUseEscKey = true;

        GameInputLock.Unlock();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void EndMiniGame()
    {
        ClearMiniGame();

        isGameOver = true;
        IsPlaying = false;

        panel.SetActive(false);

        if (UISettingsManager.Instance != null)
            UISettingsManager.Instance.canUseEscKey = true;

        GameInputLock.Unlock();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 초기화
    private void ClearMiniGame()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        foreach (var obj in spawnedBrokens)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedBrokens.Clear();
    }
}
