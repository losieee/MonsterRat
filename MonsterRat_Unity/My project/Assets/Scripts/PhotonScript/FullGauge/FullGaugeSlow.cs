using System;
using UnityEngine;

public class FullGaugeSlow : MonoBehaviour
{
    [Header("Slow Setting")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.8f;

    private PlayerController playerController;

    private float originalMoveSpeed;
    private float originalRunSpeed;

    private bool isSlowActive = false;

    void Awake()
    {
        FindController();
    }

    void FindController()
    {
        if (playerController != null)
            return;

        playerController = GetComponentInParent<PlayerController>();

        if (playerController == null)
            playerController = GetComponentInChildren<PlayerController>();

        if (playerController == null && transform.root != null)
            playerController = transform.root.GetComponentInChildren<PlayerController>();
    }

    public void StartSlow(Action onFinished = null)
    {
        if (isSlowActive) return;

        FindController();

        if (playerController == null) return;

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

        FindController();

        if (playerController == null) return;

        isSlowActive = false;

        playerController.moveSpeed = originalMoveSpeed;
        playerController.runSpeed = originalRunSpeed;
    }
}