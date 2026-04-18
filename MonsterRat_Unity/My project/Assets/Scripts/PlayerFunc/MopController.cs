using Fusion;
using System.Collections;
using UnityEngine;

public class MopController : InvenBase
{
    public override ToolType Type => ToolType.Mop;

    [SerializeField] private LayerMask pollutionMask;
    [SerializeField] private float cleanDistance = 3f;

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
            PhotonPollutionControl multiPol = hit.collider.GetComponentInParent<PhotonPollutionControl>();
            if (multiPol != null)
            {
                multiPol.CleanOnce();
                // 임시 사운드
                Rpc_PlayMopSound();

                StartCoroutine(Cooldown());
                return;
            }

            PollutionControl pol = hit.collider.GetComponentInParent<PollutionControl>();
            if (pol != null)
            {
                pol.CleanOnce();
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

        float volumeScale = HasInputAuthority ? 0.85f : 1f;
        source.PlayOneShot(mopSound, volumeScale);
    }

    IEnumerator Cooldown()
    {
        canClean = false;
        yield return new WaitForSeconds(coolTime);
        canClean = true;
    }
}
