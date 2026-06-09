using Fusion;
using UnityEngine;

public class NetworkDoor : NetworkBehaviour
{
    [SerializeField] private Animator anim;

    // 박진웅: 이게 저희가 탈출할때 쓰는 스크립트입니다.
    [SerializeField] private SafeZoneTrigger safeZone;

    public AudioSource source;
    public AudioClip clip;

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
        PlayDoorSoundOnce();
    }

    private void PlayDoorSoundOnce()
    {
        if (source == null || clip == null) return;

        UpdateOpenDoorVolume();

        source.clip = clip;
        source.time = 0f;
        source.Play();
    }

    private void UpdateOpenDoorVolume()
    {
        if (source == null) return;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        source.volume = effectVolume;
    }
}