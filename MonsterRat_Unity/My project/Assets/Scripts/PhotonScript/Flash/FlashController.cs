using Fusion;
using UnityEngine;

public class FlashController : InvenBase
{
    public override ToolType Type => ToolType.Flash;

    public GameObject flash;
    public GameObject cone;

    public AudioSource source;
    public AudioClip flashSound;

    // 손전등이 켜졌나 꺼졌나
    [Networked] public bool IsOn { get; set; }
    // 손전등 밝기
    [Networked] public float Intensity { get; set; }
    public Light spotLight;

    public override void Spawned()
    {
        Intensity = 3f;
    }

    private void OnDisable()
    {

        if (Object == null || !Object.IsValid) return;
        if (flash != null)
            flash.SetActive(IsOn);
    }

    public override void Tick()
    {
        if (!Object.HasInputAuthority) return;

        if (Input.GetKeyDown(KeyCode.T))
        {
            RPC_ToggleFlash();
            Rpc_PlayMopSound();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_PlayMopSound()
    {
        if (source == null || flashSound == null)
            return;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        float baseVolume = HasInputAuthority ? 0.85f : 1f;
        float finalVolume = baseVolume * effectVolume;

        source.PlayOneShot(flashSound, finalVolume);
    }

    public override void Render()
    {
        if (flash != null)
            flash.SetActive(IsOn);

        if (cone != null)
            cone.SetActive(IsOn);

        if (spotLight != null)
        {
            spotLight.enabled = IsOn;
            spotLight.intensity = Intensity;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_ToggleFlash()
    {
        IsOn = !IsOn;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetFlash(bool value)
    {
        IsOn = value;
    }
}
