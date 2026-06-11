using Fusion;
using UnityEngine;

public class HandOverDamage : NetworkBehaviour
{
    private PlayerGas gas;
    private PlayerHitAnim hitAnim;

    private void Awake()
    {
        gas = GetComponent<PlayerGas>();
        hitAnim = GetComponentInChildren<PlayerHitAnim>(true);
    }

    public void TakeRatHitFromStateAuthority(float damage)
    {
        if (!HasStateAuthority)
            return;

        if (gas != null)
            gas.AddExposure(damage);

        Rpc_PlayHitAnim();
    }

    public void TakeWatcherHitFromStateAuthority(float damage)
    {
        if (!HasStateAuthority)
            return;

        if (gas != null)
            gas.AddExposure(damage);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void Rpc_PlayHitAnim()
    {
        if (hitAnim != null)
            hitAnim.PlayerHit();
    }
}
