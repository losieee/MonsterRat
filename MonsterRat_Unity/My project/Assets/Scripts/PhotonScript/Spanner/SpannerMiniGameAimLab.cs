using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpannerMiniGameAimLab : MonoBehaviour
{
    [Header("Spawn Settings")]
    public List<RectTransform> spawnPoints = new List<RectTransform>();
    public RectTransform pipeBrokenPrefab;

    public float spawnInterval = 3f;
    public int totalSpawnCount = 10;

    [Header("스폰되는 박스 크기 (중앙 맞춤용)")]
    public Vector2 horizontalSize = new Vector2(160f, 60f);
    public Vector2 verticalSize = new Vector2(60f, 160f);

    private int spawnedCount = 0;

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (spawnedCount < totalSpawnCount && spawnPoints.Count > 0)
        {
            yield return new WaitForSeconds(spawnInterval);

            RectTransform point = spawnPoints[Random.Range(0, spawnPoints.Count)];

            RectTransform broken = Instantiate(
                pipeBrokenPrefab,
                point.parent
            );

            broken.position = point.position;
            broken.rotation = point.rotation;
            broken.localScale = Vector3.one;

            RectTransform pointRect = point;

            bool isHorizontal = pointRect.rect.width >= pointRect.rect.height;

            if (isHorizontal)       // 만약 스폰될 공간의 박스의 가로가 더 크면
            {
                broken.sizeDelta = horizontalSize;

                Vector2 pos = broken.anchoredPosition;
                pos.y = pointRect.anchoredPosition.y;           // Y축 중앙 고정
                broken.anchoredPosition = pos;
            }
            else
            {
                broken.sizeDelta = verticalSize;

                Vector2 pos = broken.anchoredPosition;
                pos.x = pointRect.anchoredPosition.x;           // X축 중앙 고정
                broken.anchoredPosition = pos;
            }

            spawnedCount++;
        }
    }
}
