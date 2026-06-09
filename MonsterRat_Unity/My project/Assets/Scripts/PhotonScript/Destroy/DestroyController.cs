using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyController : NetworkBehaviour
{
    private Animator anim;

    [Header("Settings")]
    public float deleteDelay = 2f;   
    public float cooldown = 3f;
    public AudioSource source;
    public AudioClip fireSound;

    public HashSet<NetworkObject> deleteBoxes = new HashSet<NetworkObject>();

    [Networked]
    public NetworkBool IsDeleting { get; set; }

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void CanDelete()
    {
        if (IsDeleting) return;
        Rpc_RequestDestroy();  
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RequestDestroy()
    {
        if (IsDeleting) return;
        IsDeleting = true;

        Rpc_PlayAnimation(); 
        StartCoroutine(DestroyRoutine()); // 호스트 본인만 타이머 시작
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlayAnimation()
    {
        if (anim != null) anim.SetTrigger("PushButton");
    }

    private IEnumerator DestroyRoutine()
    {
        yield return new WaitForSeconds(deleteDelay);

        var copy = new List<NetworkObject>(deleteBoxes);
        foreach (var netObj in copy)
        {
            if (netObj != null && netObj.IsValid)
            {
                Runner.Despawn(netObj);
            }
        }
        deleteBoxes.Clear();

        Rpc_StartFireSound();

        yield return new WaitForSeconds(cooldown);

        Rpc_StopFireSound();

        IsDeleting = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_StartFireSound()
    {
        if (source == null || fireSound == null) return;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        source.clip = fireSound;
        source.loop = true;
        source.volume = 0.5f * effectVolume;
        source.Play();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_StopFireSound()
    {
        if (source == null) return;

        source.Stop();
        source.loop = false;
    }
}