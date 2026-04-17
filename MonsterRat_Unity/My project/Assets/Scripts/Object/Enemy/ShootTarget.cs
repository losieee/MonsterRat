using Fusion;
using System.Collections;
using UnityEngine;

public class ShootTarget : NetworkBehaviour, IClearTarget
{
    public int maxHits = 3;
    public float weight = 1f;
    public bool destroyOnDeath = true;
    public NetworkPrefabRef remainPrefab;
    public NetworkPrefabRef afterGasPrefab;
    public bool snapToGround = true;
    public float rayStartHeight = 2f;
    public float rayDistance = 10f;
    public LayerMask floorMask;
    public ParticleSystem boxheadParticle;

    private int hitCount = 0;
    private bool dead = false;
    private bool registered = false;
    private bool deathHandled = false;

    public float Remain01 => dead ? 0f : 1f;
    public float Weight => weight;

    public override void Spawned()
    {
        if (ClearManager.Instance != null)
        {
            ClearManager.Instance.Register(this);
            registered = true;

            StartCoroutine(ParticleClear());
        }
    }

    private void OnDisable()
    {
        if (registered && ClearManager.Instance != null)
        {
            ClearManager.Instance.Unregister(this);
            registered = false;
        }
    }

    // 총에서 호출할 함수
    public void ApplyHit()
    {
        if (!HasStateAuthority) return;
        if (dead) return;

        hitCount++;

        if (hitCount >= maxHits)
            Die();
    }

    private void Die()
    {
        if (deathHandled) return;
        deathHandled = true;
        dead = true;

        Vector3 spawnPos = transform.position;

        if (snapToGround)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * rayStartHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, floorMask, QueryTriggerInteraction.Ignore))
            {
                spawnPos = hit.point;
            }
        }

        if (remainPrefab.IsValid)
        {
            Runner.Spawn(remainPrefab, spawnPos, transform.rotation);
        }

        // 가스 연출은 로컬 이펙트면 권한 쪽에서만 1회 생성
        if (afterGasPrefab != null)
        {
            Runner.Spawn(afterGasPrefab, transform.position, Quaternion.identity);
        }

        if (destroyOnDeath)
        {
            if (Object != null && Runner != null)
                Runner.Despawn(Object);
            else
                gameObject.SetActive(false);
        }
    }

    IEnumerator ParticleClear()
    {
        yield return null;
        boxheadParticle.Clear();
        boxheadParticle.Play();
    }
}
