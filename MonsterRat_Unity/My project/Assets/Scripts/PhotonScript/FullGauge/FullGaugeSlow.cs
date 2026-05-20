using System;
using System.Collections;
using UnityEngine;

public class FullGaugeSlow : MonoBehaviour
{
    [Header("Slow Setting")]
    public float slowDuration = 10f;

    [Range(0f, 1f)]
    public float slowMultiplier = 0.8f;         // 20% 감소

    PlayerController playerController;

    float originalMoveSpeed;
    float originalRunSpeed;

    Coroutine slowRoutine;

    void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
    }

    public void StartSlow(Action onFinished = null)
    {
        if (slowRoutine != null)
            StopCoroutine(slowRoutine);

        slowRoutine = StartCoroutine(SlowCoroutine(onFinished));
    }

    IEnumerator SlowCoroutine(Action onFinished)
    {
        if (playerController == null)
            yield break;

        // 원래 속도 저장
        originalMoveSpeed = playerController.moveSpeed;
        originalRunSpeed = playerController.runSpeed;

        // 이동속도 20% 감소
        playerController.moveSpeed = originalMoveSpeed * slowMultiplier;
        playerController.runSpeed = originalRunSpeed * slowMultiplier;

        yield return new WaitForSeconds(slowDuration);

        // 원래 속도 복구
        playerController.moveSpeed = originalMoveSpeed;
        playerController.runSpeed = originalRunSpeed;

        slowRoutine = null;

        onFinished?.Invoke();
    }
}
