using Fusion;
using UnityEngine;

public class GasValveSync : NetworkBehaviour
{
    public ParticleSystem linkedGasParticle; // 파티클 넣으세용
    [SerializeField] private GameObject fixPointRoot;
    [SerializeField] private Collider interactCollider;

    private bool _visible;

    public override void Spawned()
    {
        if (linkedGasParticle != null)
        {
            linkedGasParticle.Clear(true);
            linkedGasParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        ApplyVisibleState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (linkedGasParticle != null)
        {
            linkedGasParticle.Stop();
        }
    }

    public void SetVisible(bool visible)
    {
        if (_visible == visible)
            return;

        _visible = visible;
        ApplyVisibleState();
    }

    private void ApplyVisibleState()
    {
        if (fixPointRoot != null)
            fixPointRoot.SetActive(_visible);

        if (interactCollider != null)
            interactCollider.enabled = _visible;

        if (linkedGasParticle != null)
        {
            if (_visible)
            {
                linkedGasParticle.Clear(true);
                linkedGasParticle.Play(true);
            }
            else
            {
                linkedGasParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                linkedGasParticle.Clear(true);
            }
        }
    }

    public void FixValve()
    {
        if (!_visible) return;
        if (Object == null || !Object.IsValid) return;

        if (Object.HasStateAuthority)
        {
            NetworkObject parentNetObj = Object.transform.parent.GetComponent<NetworkObject>();

            if (parentNetObj != null && parentNetObj.IsValid)
            {
                Runner.Despawn(parentNetObj);
            }
            else
            {
                Runner.Despawn(Object);
            }
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