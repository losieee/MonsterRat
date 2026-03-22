using UnityEngine;
using Fusion;

public class PhotonHandGrabNetwork : NetworkBehaviour
{
    // 원본 그대로 RpcTargets.StateAuthority 유지! (가장 안정적)
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

                // 💡 [수정 포인트 4] 엄청난 속도값(NaN)이 들어오면 강제로 0으로 만들어 허공으로 날아가는 버그 차단!
                if (!float.IsNaN(throwVelocity.x))
                {
                    rb.linearVelocity = Vector3.ClampMagnitude(throwVelocity, 25f);
                }
                else
                {
                    rb.linearVelocity = Vector3.zero;
                }

                rb.WakeUp();
            }
        }
    }
}