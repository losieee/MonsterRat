using UnityEngine;
using Fusion;

public class PhotonHandGrabNetwork : NetworkBehaviour
{
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetGrabState(NetworkObject obj, bool isGrabbed, RpcInfo info = default)
    {
        if (obj != null)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.WakeUp();
                rb.useGravity = !isGrabbed;
                rb.isKinematic = isGrabbed;
            }

            if (isGrabbed)
            {
                if (info.Source != Runner.LocalPlayer)
                {
                    obj.AssignInputAuthority(info.Source);
                }
            }
            else
            {
                if (info.Source != Runner.LocalPlayer)
                {
                    obj.RemoveInputAuthority();
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Unreliable)]
    public void RPC_MoveObjectUnreliable(NetworkObject obj, Vector3 newPos)
    {
        if (obj != null)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null && rb.isKinematic)
            {
                rb.WakeUp();
                rb.MovePosition(newPos);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ReleaseAndThrow(NetworkObject obj, Vector3 throwVelocity, RpcInfo info = default)
    {
        if (obj != null)
        {
            if (info.Source != Runner.LocalPlayer)
            {
                obj.RemoveInputAuthority();
            }

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.freezeRotation = false;
                rb.WakeUp();
                rb.linearVelocity = throwVelocity;
            }
        }
    }
}