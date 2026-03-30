using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootTarget : MonoBehaviour, IClearTarget
{
    public int maxHits = 3;
    public float weight = 1f;
    public bool destroyOnDeath = true;
    public GameObject remainPrefab;
    public GameObject afterGasPrefab;
    public bool snapToGround = true;
    public float groundY = 0f;

    private int hitCount = 0;
    private bool dead = false;

    public float Remain01 => dead ? 0f : 1f;
    public float Weight => weight;

    void OnEnable()
    {
        if (ClearManager.Instance != null)
            ClearManager.Instance.Register(this);
    }

    void OnDisable()
    {
        if (ClearManager.Instance != null)
            ClearManager.Instance.Unregister(this);

        if (afterGasPrefab != null)
            Instantiate(afterGasPrefab, transform.position, default);
    }

    // 총에서 호출할 함수
    public void ApplyHit()
    {
        if (dead) return;

        hitCount++;

        if (hitCount >= maxHits)
            Die();
    }

    void Die()
    {
        dead = true;

        if (remainPrefab != null)
        {
            Vector3 pos = transform.position;
            if (snapToGround) pos.y = groundY;
            Instantiate(remainPrefab, pos, transform.rotation);
        }

        Destroy(gameObject);
    }
}
