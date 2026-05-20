using System;
using System.Collections;
using UnityEngine;

public class FullGaugeHeadShake : MonoBehaviour
{
    [Header("Target")]
    public Transform cameraTransform;

    [Header("Shake Setting")]
    public float fadeInDuration = 2f;           // 진입 시간
    public float holdDuration = 6f;
    public float fadeOutDuration = 2f;          // 마감 시간

    public float rotationAmount = 2f;           // 카메라 흔들림
    public float positionAmount = 0.03f;        // 카메라 흔들리는 중심 움직임
    public float shakeSpeed = 8f;               // 움직임 속도
    public float zoomOutAmount = 15f;           // 기본이 60이면 +15 까지 줌인했다 줄어듬
    public float zoomSpeed = 2f;                // 줌 속도

    Coroutine shakeRoutine;

    Quaternion originalLocalRotation;
    Vector3 originalLocalPosition;

    PlayerController playerController;

    void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
    }

    public void StartShake(Action onFinished = null)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeCoroutine(onFinished));
    }

    IEnumerator ShakeCoroutine(Action onFinished)
    {
        if (playerController == null)
            yield break;

        float totalDuration = fadeInDuration + holdDuration + fadeOutDuration;
        float time = 0f;

        while (time < totalDuration)
        {
            time += Time.deltaTime;

            float intensity = GetIntensity(time);

            // 회전
            float xRot = Mathf.Sin(Time.time * shakeSpeed) * rotationAmount * intensity;
            float zRot = Mathf.Cos(Time.time * shakeSpeed * 0.7f) * rotationAmount * intensity;

            // 위치
            float xPos = Mathf.Sin(Time.time * shakeSpeed * 0.8f) * positionAmount * intensity;
            float yPos = Mathf.Cos(Time.time * shakeSpeed * 0.6f) * positionAmount * intensity;

            // 줌인
            float zoomWave = Mathf.Sin(Time.time * zoomSpeed) * 0.5f + 0.5f;
            float fovOffset = zoomWave * zoomOutAmount * intensity;

            playerController.cameraEffectEuler = new Vector3(xRot, 0f, zRot);
            playerController.cameraEffectPosition = new Vector3(xPos, yPos, 0f);
            playerController.cameraEffectFovOffset = fovOffset;

            yield return null;
        }

        playerController.cameraEffectEuler = Vector3.zero;
        playerController.cameraEffectPosition = Vector3.zero;
        playerController.cameraEffectFovOffset = 0f;

        shakeRoutine = null;

        onFinished?.Invoke();
    }

    float GetIntensity(float time)
    {
        // 서서히 시작
        if (time < fadeInDuration)
        {
            float t = time / fadeInDuration;
            return Mathf.SmoothStep(0f, 1f, t);
        }

        // 유지
        if (time < fadeInDuration + holdDuration)
        {
            return 1f;
        }

        // 서서히 종료
        float fadeOutTime = time - fadeInDuration - holdDuration;
        float fadeOutT = fadeOutTime / fadeOutDuration;

        return Mathf.SmoothStep(1f, 0f, fadeOutT);
    }
}
