using UnityEngine;
using Fusion;

public class PhotonHandGrab : InvenBase
{
    public override ToolType Type => ToolType.Hand;

    [Header("Grab")]
    public float grabHoldDistance = 3f;
    public float grabMoveSpeed = 15f;
    public float throwBoost = 2.5f;
    public LayerMask grabBlock;
    public float grabPadding = 0.05f;
    public float minHoldDistance = 0.6f;

    NetworkObject targetNetObj;
    Rigidbody targetRb;
    Vector3 lastGrabPos;
    Vector3 lastGrabVel;
    float grabbedRadius = 0.25f;

    Vector3 centerOffset;
    Vector3 virtualGrabPos;

    PhotonHandGrabNetwork netBridge;
    NetworkObject playerNetObj;

    public override void Init(PlayerUIState uiState, PlayerRaycast playerInteractor)
    {
        base.Init(uiState, playerInteractor);
        netBridge = transform.root.GetComponent<PhotonHandGrabNetwork>();
        playerNetObj = transform.root.GetComponent<NetworkObject>();
    }

    public override void Tick()
    {
        bool isLocalPlayer = playerNetObj != null && (playerNetObj.HasStateAuthority || playerNetObj.HasInputAuthority);
        if (!isLocalPlayer || netBridge == null || interactor == null) return;

        if (Input.GetMouseButtonDown(1))
        {
            GameObject t = interactor.LookTarget;

            if (t == null || t.layer != 3)
            {
                int boxLayerMask = 1 << 3;
                if (Physics.SphereCast(interactor.cam.position, 0.4f, interactor.cam.forward, out RaycastHit hit, grabHoldDistance, boxLayerMask))
                {
                    t = hit.collider.gameObject;
                }
            }

            if (t != null && t.layer == 3)
            {
                TryGrab(t);
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            Release();
        }
    }

    public override void FixedTick()
    {
        bool isLocalPlayer = playerNetObj != null && (playerNetObj.HasStateAuthority || playerNetObj.HasInputAuthority);
        if (!isLocalPlayer) return;

        if (targetRb != null && targetNetObj != null)
        {
            MoveGrabbedObject();
        }
    }

    void TryGrab(GameObject target)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        NetworkObject netObj = target.GetComponent<NetworkObject>();

        if (rb == null || netObj == null) return;

        targetRb = rb;
        targetNetObj = netObj;
        netBridge.RPC_SetGrabState(targetNetObj, true);

        Collider col = targetRb.GetComponent<Collider>();
        if (col != null)
        {
            Vector3 e = col.bounds.extents;
            grabbedRadius = Mathf.Min(e.x, e.y, e.z);
            if (grabbedRadius > 0.3f) grabbedRadius = 0.3f;
            centerOffset = targetRb.transform.InverseTransformPoint(col.bounds.center);
        }
        else
        {
            grabbedRadius = 0.25f;
            centerOffset = Vector3.zero;
        }

        virtualGrabPos = targetRb.position;
        lastGrabPos = virtualGrabPos;
        lastGrabVel = Vector3.zero;
    }

    void Release()
    {
        if (targetRb == null || targetNetObj == null || netBridge == null) return;

        if (interactor != null && interactor.cam != null)
        {
            Vector3 throwVelocity = lastGrabVel + interactor.cam.forward * throwBoost;
            netBridge.RPC_ReleaseAndThrow(targetNetObj, throwVelocity);
        }

        targetRb = null;
        targetNetObj = null;
    }

    void MoveGrabbedObject()
    {
        if (interactor == null || interactor.cam == null || targetRb == null || targetNetObj == null) return;

        bool hasControl = targetNetObj.HasStateAuthority || targetNetObj.HasInputAuthority;

        if (!hasControl)
        {
            virtualGrabPos = targetRb.position;
            lastGrabPos = targetRb.position;
            return;
        }

        float desiredDist = grabHoldDistance;
        float actualDist = desiredDist;
        if (Physics.SphereCast(interactor.cam.position, grabbedRadius, interactor.cam.forward,
            out RaycastHit hit, desiredDist, grabBlock, QueryTriggerInteraction.Ignore))
        {
            actualDist = Mathf.Clamp(hit.distance - grabPadding, minHoldDistance, desiredDist);
        }

        Vector3 targetCenterPos = interactor.cam.position + interactor.cam.forward * actualDist;
        Vector3 worldCenterOffset = targetRb.transform.TransformDirection(centerOffset);
        Vector3 desiredPivotPos = targetCenterPos - worldCenterOffset;

        Vector3 toTarget = desiredPivotPos - virtualGrabPos;
        Vector3 newPos = virtualGrabPos + toTarget * grabMoveSpeed * Time.fixedDeltaTime;

        lastGrabVel = (newPos - lastGrabPos) / Time.fixedDeltaTime;
        lastGrabPos = newPos;

        virtualGrabPos = newPos;

        if (targetNetObj.HasStateAuthority)
        {
            targetRb.MovePosition(newPos);
        }
        else
        {
            targetRb.transform.position = newPos;
            netBridge.RPC_MoveObjectUnreliable(targetNetObj, newPos);
        }
    }
}