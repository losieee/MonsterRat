using System;
using System.Collections;
using UnityEngine;

public class FullGaugeBlind : MonoBehaviour
{
    public Camera targetCamera;

    [Header("Blind Setting")]
    public float blindFadeInDuration = 2f;          // 서서히 어두워짐
    public float blindDuration = 10f;               // 실명 유지 시간
    public float visibleDistance = 1.5f;            // 어디까지 보일지
    public float fogStartDistance = 0.2f;           // 어두워지기 시작할 지점
    public float recoverDuration = 3f;              // 기존 시야 찾기까지 시간
    public float recoverEndDistance = 80f;          // 어디까지 넓힐건지

    bool originalFog;
    Color originalFogColor;
    FogMode originalFogMode;
    float originalFogStart;
    float originalFogEnd;
    float originalFogDensity;

    Coroutine blindRoutine;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    public void StartBlind(Action onFinished = null)
    {
        if (blindRoutine != null)
            StopCoroutine(blindRoutine);

        blindRoutine = StartCoroutine(BlindCoroutine(onFinished));
    }

    IEnumerator BlindCoroutine(Action onFinished)
    {
        // 기존 안개 값 저장
        originalFog = RenderSettings.fog;
        originalFogColor = RenderSettings.fogColor;
        originalFogMode = RenderSettings.fogMode;
        originalFogStart = RenderSettings.fogStartDistance;
        originalFogEnd = RenderSettings.fogEndDistance;
        originalFogDensity = RenderSettings.fogDensity;

        // 실명 효과를 위해 Fog 켜기
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogMode = FogMode.Linear;

        // 시작 지점 설정
        // 원래 Fog가 켜져 있었다면 원래 값에서 시작
        // 원래 Fog가 꺼져 있었다면 넓은 시야 상태에서 시작
        float startFogStart = originalFog ? originalFogStart : recoverEndDistance * 0.8f;
        float startFogEnd = originalFog ? originalFogEnd : recoverEndDistance;

        float targetFogStart = fogStartDistance;
        float targetFogEnd = visibleDistance;

        RenderSettings.fogStartDistance = startFogStart;
        RenderSettings.fogEndDistance = startFogEnd;

        // 서서히 실명
        float time = 0f;

        while (time < blindFadeInDuration)
        {
            time += Time.deltaTime;

            float t = time / blindFadeInDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            RenderSettings.fogStartDistance = Mathf.Lerp(startFogStart, targetFogStart, t);
            RenderSettings.fogEndDistance = Mathf.Lerp(startFogEnd, targetFogEnd, t);

            yield return null;
        }

        // 실명 상태 고정
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fogEndDistance = visibleDistance;

        // 실명 상태 유지
        yield return new WaitForSeconds(blindDuration);

        // 서서히 시야 회복
        time = 0f;

        float recoverStartFogStart = fogStartDistance;
        float recoverStartFogEnd = visibleDistance;

        float recoverTargetFogStart = originalFog ? originalFogStart : recoverEndDistance * 0.8f;
        float recoverTargetFogEnd = originalFog ? originalFogEnd : recoverEndDistance;

        while (time < recoverDuration)
        {
            time += Time.deltaTime;

            float t = time / recoverDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            RenderSettings.fogStartDistance = Mathf.Lerp(recoverStartFogStart, recoverTargetFogStart, t);
            RenderSettings.fogEndDistance = Mathf.Lerp(recoverStartFogEnd, recoverTargetFogEnd, t);

            yield return null;
        }

        // 원래 안개 값 복구
        RenderSettings.fog = originalFog;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogMode = originalFogMode;
        RenderSettings.fogStartDistance = originalFogStart;
        RenderSettings.fogEndDistance = originalFogEnd;
        RenderSettings.fogDensity = originalFogDensity;

        blindRoutine = null;

        onFinished?.Invoke();
    }
}