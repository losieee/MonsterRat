using System;
using System.Collections;
using UnityEngine;

public class FullGaugeBlind : MonoBehaviour
{
    public Camera targetCamera;

    [Header("Blind Setting")]
    public float blindFadeInDuration = 2f;
    public float visibleDistance = 1.5f;
    public float fogStartDistance = 0.2f;
    public float recoverDuration = 3f;
    public float recoverEndDistance = 80f;

    bool originalFog;
    Color originalFogColor;
    FogMode originalFogMode;
    float originalFogStart;
    float originalFogEnd;
    float originalFogDensity;

    bool savedOriginal = false;
    bool isBlindActive = false;

    Coroutine blindRoutine;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    public void StartBlind(Action onFinished = null)
    {
        if (isBlindActive)
            return;

        isBlindActive = true;

        if (blindRoutine != null)
            StopCoroutine(blindRoutine);

        blindRoutine = StartCoroutine(BlindInCoroutine());
    }

    IEnumerator BlindInCoroutine()
    {
        SaveOriginalFog();

        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogMode = FogMode.Linear;

        float startFogStart = originalFog ? originalFogStart : recoverEndDistance * 0.8f;
        float startFogEnd = originalFog ? originalFogEnd : recoverEndDistance;

        float time = 0f;

        while (time < blindFadeInDuration)
        {
            time += Time.deltaTime;

            float t = time / blindFadeInDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            RenderSettings.fogStartDistance = Mathf.Lerp(startFogStart, fogStartDistance, t);
            RenderSettings.fogEndDistance = Mathf.Lerp(startFogEnd, visibleDistance, t);

            yield return null;
        }

        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fogEndDistance = visibleDistance;

        blindRoutine = null;
    }

    public void StopBlind()
    {
        if (!isBlindActive)
            return;

        isBlindActive = false;

        if (blindRoutine != null)
            StopCoroutine(blindRoutine);

        blindRoutine = StartCoroutine(BlindOutCoroutine());
    }

    IEnumerator BlindOutCoroutine()
    {
        float time = 0f;

        float startFogStart = RenderSettings.fogStartDistance;
        float startFogEnd = RenderSettings.fogEndDistance;

        float targetFogStart = originalFog ? originalFogStart : recoverEndDistance * 0.8f;
        float targetFogEnd = originalFog ? originalFogEnd : recoverEndDistance;

        while (time < recoverDuration)
        {
            time += Time.deltaTime;

            float t = time / recoverDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            RenderSettings.fogStartDistance = Mathf.Lerp(startFogStart, targetFogStart, t);
            RenderSettings.fogEndDistance = Mathf.Lerp(startFogEnd, targetFogEnd, t);

            yield return null;
        }

        RestoreOriginalFog();

        savedOriginal = false;
        blindRoutine = null;
    }

    void SaveOriginalFog()
    {
        if (savedOriginal)
            return;

        originalFog = RenderSettings.fog;
        originalFogColor = RenderSettings.fogColor;
        originalFogMode = RenderSettings.fogMode;
        originalFogStart = RenderSettings.fogStartDistance;
        originalFogEnd = RenderSettings.fogEndDistance;
        originalFogDensity = RenderSettings.fogDensity;

        savedOriginal = true;
    }

    void RestoreOriginalFog()
    {
        RenderSettings.fog = originalFog;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogMode = originalFogMode;
        RenderSettings.fogStartDistance = originalFogStart;
        RenderSettings.fogEndDistance = originalFogEnd;
        RenderSettings.fogDensity = originalFogDensity;
    }
}