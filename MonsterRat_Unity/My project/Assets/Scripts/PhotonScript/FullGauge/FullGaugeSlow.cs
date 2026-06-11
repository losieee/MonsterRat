using System;
using UnityEngine;

public class FullGaugeSlow : MonoBehaviour
{
    [Header("Slow Setting")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.8f;

    PlayerController playerController;

    float originalMoveSpeed;
    float originalRunSpeed;

    bool isSlowActive = false;

    void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
    }

    public void StartSlow(Action onFinished = null)
    {
        if (isSlowActive)
            return;

        if (playerController == null)
            return;

        isSlowActive = true;

        originalMoveSpeed = playerController.moveSpeed;
        originalRunSpeed = playerController.runSpeed;

        playerController.moveSpeed = originalMoveSpeed * slowMultiplier;
        playerController.runSpeed = originalRunSpeed * slowMultiplier;
    }

    public void StopSlow()
    {
        if (!isSlowActive)
            return;

        if (playerController == null)
            return;

        isSlowActive = false;

        playerController.moveSpeed = originalMoveSpeed;
        playerController.runSpeed = originalRunSpeed;
    }
}