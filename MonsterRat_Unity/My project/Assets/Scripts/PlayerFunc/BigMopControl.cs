using Fusion;
using System.Collections;
using UnityEngine;

public class BigMopControl : InvenBase
{
    public override ToolType Type => ToolType.BigMop;

    [SerializeField] private LayerMask pollutionMask;
    [SerializeField] private float cleanDistance = 3f;
    [SerializeField] private int cleanAmount = 1;

    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip mopSound;

    public float coolTime = 1f;
    bool canClean = true;

    public override void Tick()
    {
        if (!canClean) return;
        if (Input.GetMouseButtonDown(0))
        {
            TryClean();
        }
    }

    void TryClean()
    {
        if (interactor == null || interactor.cam == null)
            return;

        Ray ray = new Ray(interactor.cam.position, interactor.cam.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, cleanDistance, pollutionMask, QueryTriggerInteraction.Ignore))
        {
            // ÀÏ¹Ý ¾ó·è
            PhotonPollutionControl multiPol = hit.collider.GetComponentInParent<PhotonPollutionControl>();
            if (multiPol != null)
            {
                multiPol.CleanOnce(cleanAmount);
                // ÀÓ½Ã »ç¿îµå
                Rpc_PlayMopSound();

                StartCoroutine(Cooldown());
                return;
            }

            // ¿À¿° ÀÜÇâ ¾ó·è
            PhotonRePollutionControl multiRePol = hit.collider.GetComponentInParent<PhotonRePollutionControl>();
            if (multiRePol != null)
            {
                multiRePol.CleanOnce(cleanAmount);
                // ÀÓ½Ã »ç¿îµå
                Rpc_PlayMopSound();

                StartCoroutine(Cooldown());
                return;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_PlayMopSound()
    {
        if (source == null || mopSound == null)
            return;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        float baseVolume = HasInputAuthority ? 0.85f : 1f;
        float finalVolume = baseVolume * effectVolume;

        source.PlayOneShot(mopSound, finalVolume);
    }

    IEnumerator Cooldown()
    {
        canClean = false;
        yield return new WaitForSeconds(coolTime);
        canClean = true;
    }
}
