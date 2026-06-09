using Fusion;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class MonsterWatcher : NetworkBehaviour
{
    public float watcherDamage = 8f;

    public AudioSource source;
    public AudioClip watcherSound;

    [Networked] public bool canAttack { get; set; }
    [Networked] public int watcherCount { get; set; }       // 왓쳐를 보고있는 플레이어
    private HashSet<PlayerRef> watchingPlayers = new HashSet<PlayerRef>();

    private Transform target;

    public override void Spawned()
    {
        PhotonPlayerUIState.Local?.ShowWatcherWarning();

        if (Object.HasStateAuthority)
        {
            canAttack = false;
            StartCoroutine(Init());
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        PhotonPlayerUIState.Local?.HideWatcherWarning();
    }

    private void Update()
    {
        UpdateWatcherVolume();
    }

    private void UpdateWatcherVolume()
    {
        if (source == null) return;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        source.volume = 0.5f * effectVolume;
    }

    IEnumerator Init()
    {
        yield return new WaitForSeconds(0.5f);

        FindRandomPlayer();
        StartCoroutine(DespawnAfterTime(35f));
        StartCoroutine(TimeAttack(10f));
    }

    void FindRandomPlayer()
    {
        // 씬에 있는 모든 Player 찾고
        var players = FindObjectsOfType<PlayerController>();

        if (players.Length == 0) return;

        // 그 중 랜덤으로 선택
        int rand = Random.Range(0, players.Length);
        target = players[rand].transform;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (target == null) return;

        // 플레이어에게 시선 고정
        Vector3 dir = (target.position - transform.position);
        dir.y = 0f;

        // 플레이어 쪽으로 회전
        if (dir.sqrMagnitude > 0.001f)
        {
            dir.Normalize();
            transform.forward = Vector3.Lerp(transform.forward, dir, Runner.DeltaTime * 5f);
        }

        CheckFlashLight();

        // 플레이어를 쳐다보는 중 (플레이어 중독수치 증가)
        if (canAttack && watchingPlayers.Count == 0)
        {
            HandOverDamage receiver = target.GetComponent<HandOverDamage>();
            if (receiver != null)
            {
                receiver.Rpc_TakeWatcherHit(watcherDamage * Runner.DeltaTime);
            }
        }
    }

    // 지금 왓쳐 상태 (손전등 비춰지고 있는 중인가)
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_AddWatcher(PlayerRef player)
    {
        watchingPlayers.Add(player);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RemoveWatcher(PlayerRef player)
    {
        watchingPlayers.Remove(player);
    }

    void CheckFlashLight()
    {
        watchingPlayers.Clear();

        foreach (var player in Runner.ActivePlayers)
        {
            var obj = Runner.GetPlayerObject(player);
            if (obj == null) continue;

            FlashRange flash = obj.GetComponentInChildren<FlashRange>();
            if (flash == null || !flash.IsActive) continue;

            Vector3 dir = (transform.position - flash.transform.position);
            float dist = dir.magnitude;

            if (dist > flash.range) continue;

            dir.Normalize();

            float deg = Vector3.Angle(flash.transform.forward, dir);

            if (deg <= flash.angle)
            {
                watchingPlayers.Add(player);
            }
        }
    }

    // 공격하기까지 10초전
    IEnumerator TimeAttack(float time)
    {
        yield return new WaitForSeconds(time);

        if (Object.HasStateAuthority)
        {
            canAttack = true;
        }
    }

    IEnumerator DespawnAfterTime(float time)
    {
        yield return new WaitForSeconds(time);

        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}