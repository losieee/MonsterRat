using Fusion;
using UnityEngine;

public class GasValveSync : NetworkBehaviour
{
    public ParticleSystem linkedGasParticle; // 파티클 넣으세용

    public override void Spawned()
    {
        if (linkedGasParticle != null) linkedGasParticle.Play();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (linkedGasParticle != null) linkedGasParticle.Stop();
    }

    public void FixValve()
    {
        if (Object == null || !Object.IsValid) return;

        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
        else
        {
            RPC_RequestFix();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] // mopcontroller이랑 비슷한 개념입니다. PhotonSpanner은 InvenBase 받아오되 이 스크립트는 Fusion2 전용으로
    public void RPC_RequestFix()
    {
        if (Object != null && Object.IsValid)
        {
            Runner.Despawn(Object);
        }
    }
}