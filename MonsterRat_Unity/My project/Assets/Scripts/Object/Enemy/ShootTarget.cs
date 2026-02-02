using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootTarget : MonoBehaviour, IClearTarget
{
    public int maxHits = 3;
    public bool destroyOnDeath = true;
    public GameObject remainPrefab;
    public bool snapToGround = true;
    public float groundY = 0f;

    private int hitCount = 0;
    private bool dead = false;

    public float Remain01 => dead ? 0f : 1f;

    void OnEnable()
    {
        if (ClearManager.Instance != null)
            ClearManager.Instance.Register(this);
    }

    void OnDisable()
    {
        if (ClearManager.Instance != null)
            ClearManager.Instance.Unregister(this);
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

        Destroy(transform.parent.gameObject);
    }
}
