using UnityEngine;
using Fusion;

public class StandaloneHandGrab : NetworkBehaviour
{
    [Header("Gravity Gun Settings")]
    public float grabRange = 10f;
    public float holdDistance = 2f;
    public float pullSpeed = 15f;
    public LayerMask interactLayer;

    [Header("References")]
    public Transform aimTransform;

    [Networked] public NetworkObject GrabbedObject { get; set; }

    private Rigidbody grabbedRb;

    private void Update()
    {
        if (!HasInputAuthority) return;

        if (Input.GetMouseButtonDown(1))
        {
            TryGrabLocal();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            if (GrabbedObject != null)
            {
                Rpc_ReleaseObject();
            }
        }
    }

    private void TryGrabLocal()
    {
        Ray ray = new Ray(aimTransform.position, aimTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, grabRange, interactLayer))
        {
            if (hit.collider.CompareTag("Box"))
            {
                NetworkObject netObj = hit.collider.GetComponentInParent<NetworkObject>();
                if (netObj != null)
                {
                    Rpc_TryGrabObject(netObj);
                }
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_TryGrabObject(NetworkObject targetObj)
    {
        if (targetObj != null)
        {
            GrabbedObject = targetObj;

            Rigidbody rb = targetObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // 호스트에서 물리 연산 끄기 (잡힘)
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_ReleaseObject()
    {
        if (GrabbedObject != null)
        {
            Rigidbody rb = GrabbedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false; // 호스트에서 물리 연산 켜기 (떨어짐)
            }
            GrabbedObject = null;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // 오직 호스트만 물건을 이동시킵니다. 클라이언트는 이 결과를 껍데기로 받습니다.
        if (HasStateAuthority && GrabbedObject != null)
        {
            if (grabbedRb == null || grabbedRb.gameObject != GrabbedObject.gameObject)
            {
                grabbedRb = GrabbedObject.GetComponent<Rigidbody>();
            }

            if (grabbedRb != null)
            {
                Vector3 targetPosition = aimTransform.position + aimTransform.forward * holdDistance;
                Vector3 newPosition = Vector3.Lerp(grabbedRb.position, targetPosition, Runner.DeltaTime * pullSpeed);

                grabbedRb.MovePosition(newPosition);
                grabbedRb.MoveRotation(Quaternion.Slerp(grabbedRb.rotation, aimTransform.rotation, Runner.DeltaTime * pullSpeed));
            }
        }
    }
}