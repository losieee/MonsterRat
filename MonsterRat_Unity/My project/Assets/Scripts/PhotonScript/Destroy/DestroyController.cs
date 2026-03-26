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

        yield return new WaitForSeconds(cooldown);
        IsDeleting = false;
    }
}