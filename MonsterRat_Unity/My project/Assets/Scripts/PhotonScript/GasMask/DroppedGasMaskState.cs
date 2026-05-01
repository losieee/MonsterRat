using Fusion;
using UnityEngine;

public class DroppedGasMaskState : NetworkBehaviour
{
    [Networked] public float CooldownRemaining { get; set; }

    public void SetCooldown(float remaining)
    {
        if (HasStateAuthority)
        {
            CooldownRemaining = Mathf.Max(0f, remaining);
        }
        else
        {
            RPC_SetCooldown(remaining);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetCooldown(float remaining)
    {
        CooldownRemaining = Mathf.Max(0f, remaining);
    }
}
