using Fusion;
using UnityEngine;

public class NetworkDoor : NetworkBehaviour
{
    [SerializeField] private Animator anim;

    public void TryOpenDoor()
    {
        if (HasStateAuthority)
        {
            RPC_PlayDoorAnimation();
        }
        else
        {
            RPC_RequestOpenDoor();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestOpenDoor()
    {
        RPC_PlayDoorAnimation();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayDoorAnimation()
    {
        if (anim == null) return;
        anim.SetTrigger("ClearDoorOpen");
    }
}
