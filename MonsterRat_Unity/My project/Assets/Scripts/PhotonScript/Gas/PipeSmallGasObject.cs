using System.Collections;
using UnityEngine;
using Fusion;

public class PipeSmallGasObject : NetworkBehaviour
{
    [Header("Effect")]
    public ParticleSystem gasEffect;
    public GasZone gasZone;
    
    private BoxCollider boxcol;

    private void Awake()
    {
        boxcol = GetComponent<BoxCollider>();
        gasZone = GetComponent<GasZone>();
        if (gasEffect == null)
            gasEffect = GetComponentInChildren<ParticleSystem>();
    }

    public override void Spawned()
    {
        StartCoroutine(RefreshCollider());
    }

    IEnumerator RefreshCollider()
    {
        yield return null;

        if (boxcol != null)
        {
            boxcol.enabled = false;
            boxcol.enabled = true;
        }
    }

    public void StopGas()
    {
        if (Object == null || !Object.IsValid) return;

        if (Object.HasStateAuthority)
            RPC_SetGasEffect(false);
        else
            RPC_SetGasEffect(false);
    }

    public void PlayGas()
    {
        if (Object == null || !Object.IsValid) return;

        if (Object.HasStateAuthority)
            RPC_SetGasEffect(true);
        else
            RPC_SetGasEffect(true);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetGasEffect(bool play)
    {
        if (gasEffect != null)
        {
            var emission = gasEffect.emission;
            emission.enabled = play;

            if (play && !gasEffect.isPlaying)
                gasEffect.Play();
        }

        if (!play)
            gasZone.minParticleCount = 100;
        else
            gasZone.minParticleCount = 5;

    }
}
