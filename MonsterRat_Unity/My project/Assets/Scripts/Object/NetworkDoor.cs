using Fusion;
using UnityEngine;

public class NetworkDoor : NetworkBehaviour
{
    [SerializeField] private Animator anim;

    // 박진웅: 이게 저희가 탈출할때 쓰는 스크립트입니다.
    [SerializeField] private SafeZoneTrigger safeZone;

    public void TryOpenDoor()
    {
        if (HasStateAuthority)
        {
            RPC_PlayDoorAnimation();

           //문 애니메이션 발동될때 그냥 낑겨서 Safezonetrigger의 Isdooropened 불 값 true되게끔
            if (safeZone != null) safeZone.OpenSafeZone();
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

        //클라도 적용되게끔
        if (safeZone != null) safeZone.OpenSafeZone();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayDoorAnimation()
    {
        if (anim == null) return;
        anim.SetTrigger("ClearDoorOpen");
    }
}