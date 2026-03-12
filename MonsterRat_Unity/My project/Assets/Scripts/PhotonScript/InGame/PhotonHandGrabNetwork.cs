using UnityEngine;
using Fusion;

public class PhotonHandGrabNetwork : NetworkBehaviour
{
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetGrabState(NetworkObject obj, bool isGrabbed)
    {
        if (obj != null)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = !isGrabbed;
                rb.freezeRotation = isGrabbed;
                if (!isGrabbed) rb.WakeUp();
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_MoveObject(NetworkObject obj, Vector3 newPos)
    {
        if (obj != null)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.MovePosition(newPos); // 서버 물리 엔진이 부드럽게 이동시킴
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ThrowObject(NetworkObject obj, Vector3 throwVelocity)
    {
        if (obj != null)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                rb.freezeRotation = false;
                rb.linearVelocity = throwVelocity; // 서버가 직접 힘을 가함
            }
        }
    }
}