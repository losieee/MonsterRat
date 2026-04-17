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

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void Rpc_TakeRatHit(float damage)
    {
        if (gas != null)
            gas.AddExposure(damage);

        if (hitAnim != null)
            hitAnim.PlayerHit();
    }
}
