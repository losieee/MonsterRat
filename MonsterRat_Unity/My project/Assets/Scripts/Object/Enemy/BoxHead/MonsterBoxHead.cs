using Fusion;
using System.Collections;
using UnityEngine;

public class MonsterBoxHead : NetworkBehaviour, IClearTarget
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

    public AudioSource source;
    public AudioClip boxheadSound;

    private int hitCount = 0;
    private bool dead = false;
    private bool registered = false;
    private bool deathHandled = false;

    [Networked] public int boxHitCount { get; set; }


    public float Remain01 => dead ? 0f : 1f;
    public float Weight => weight;

    public override void Spawned()
    {
        Physics.SyncTransforms();       // <-- 박스헤드 자체 transform은 생성 됐는데
                                        // 콜라이더는 이 위치를 못따라와서 이상한곳에 생성 (이거땜에 게스트가 안맞았던거임)
                                        // 진짜 매우매우매우매우 중요한 부분 별 5개 ★★★★★

        if (ClearManager.Instance != null)
        {
            ClearManager.Instance.Register(this);
            registered = true;
            StartCoroutine(ParticleClear());
        }
    }

    private void Update()
    {
        UpdateBoxHeadVolume();
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
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RequestHit(int amount)
    {
        if (dead) return;

        hitCount += amount;

        if (hitCount >= maxHits)
            Die();
    }

    private void UpdateBoxHeadVolume()
    {
        if (source == null) return;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        source.volume = 0.5f * effectVolume;
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
        if (afterGasPrefab.IsValid)
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
