using System;
using System.Collections;
using UnityEngine;

public class FullGaugeHeadShake : MonoBehaviour
{
    [Header("Shake Setting")]
    public float fadeInDuration = 2f;
    public float fadeOutDuration = 2f;

    public float rotationAmount = 2f;
    public float positionAmount = 0.03f;
    public float shakeSpeed = 8f;
    public float zoomOutAmount = 15f;
    public float zoomSpeed = 2f;

    Coroutine shakeRoutine;
    Coroutine stopRoutine;

    PlayerController playerController;

    bool isShakeActive = false;
    float currentIntensity = 0f;

    void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
    }

    public void StartShake(Action onFinished = null)
    {
        if (isShakeActive)
            return;

        if (playerController == null)
            return;

        isShakeActive = true;

        if (stopRoutine != null)
            StopCoroutine(stopRoutine);

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeCoroutine());
    }

    IEnumerator ShakeCoroutine()
    {
        float time = 0f;

        while (time < fadeInDuration)
        {
            time += Time.deltaTime;

            float t = time / fadeInDuration;
            currentIntensity = Mathf.SmoothStep(0f, 1f, t);

            ApplyShake(currentIntensity);

            yield return null;
        }

        currentIntensity = 1f;

        while (isShakeActive)
        {
            ApplyShake(currentIntensity);
            yield return null;
        }

        shakeRoutine = null;
    }

    public void StopShake()
    {
        if (!isShakeActive)
            return;

        isShakeActive = false;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        if (stopRoutine != null)
            StopCoroutine(stopRoutine);

        stopRoutine = StartCoroutine(StopShakeCoroutine());
    }

    IEnumerator StopShakeCoroutine()
    {
        float startIntensity = currentIntensity;
        float time = 0f;

        while (time < fadeOutDuration)
        {
            time += Time.deltaTime;

            float t = time / fadeOutDuration;
            currentIntensity = Mathf.Lerp(startIntensity, 0f, Mathf.SmoothStep(0f, 1f, t));

            ApplyShake(currentIntensity);

            yield return null;
        }

        currentIntensity = 0f;

        playerController.cameraEffectEuler = Vector3.zero;
        playerController.cameraEffectPosition = Vector3.zero;
        playerController.cameraEffectFovOffset = 0f;

        stopRoutine = null;
    }

    void ApplyShake(float intensity)
    {
        if (playerController == null)
            return;

        float xRot = Mathf.Sin(Time.time * shakeSpeed) * rotationAmount * intensity;
        float zRot = Mathf.Cos(Time.time * shakeSpeed * 0.7f) * rotationAmount * intensity;

        float xPos = Mathf.Sin(Time.time * shakeSpeed * 0.8f) * positionAmount * intensity;
        float yPos = Mathf.Cos(Time.time * shakeSpeed * 0.6f) * positionAmount * intensity;

        float zoomWave = Mathf.Sin(Time.time * zoomSpeed) * 0.5f + 0.5f;
        float fovOffset = zoomWave * zoomOutAmount * intensity;

        playerController.cameraEffectEuler = new Vector3(xRot, 0f, zRot);
        playerController.cameraEffectPosition = new Vector3(xPos, yPos, 0f);
        playerController.cameraEffectFovOffset = fovOffset;
    }
}