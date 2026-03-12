using UnityEngine;
using Fusion;

public class PhotonHandGrab : TutorialInvenBase
{
    public override TutorialToolType Type => TutorialToolType.Hand;

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

    PhotonHandGrabNetwork netBridge;
    NetworkObject playerNetObj;

    public override void Init(PlayerUIState uiState, PlayerRaycast playerInteractor)
    {
        base.Init(uiState, playerInteractor);
        netBridge = GetComponent<PhotonHandGrabNetwork>();
        playerNetObj = transform.root.GetComponent<NetworkObject>();
    }

    public override void Tick()
    {
        if (playerNetObj == null || !playerNetObj.HasInputAuthority || netBridge == null) return;
        if (interactor == null) return;

        if (Input.GetMouseButtonDown(1))
        {
            GameObject t = interactor.LookTarget;
            if (t != null && t.layer == 3)
                TryGrab(t);
        }

        if (Input.GetMouseButtonUp(1))
        {
            Release();
        }
    }

    public override void FixedTick()
    {
        if (playerNetObj == null || !playerNetObj.HasInputAuthority) return;

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
            grabbedRadius = Mathf.Max(e.x, e.y, e.z);
        }
        else
            grabbedRadius = 0.25f;

        lastGrabPos = targetRb.position;
        lastGrabVel = Vector3.zero;
    }

    void Release()
    {
        if (targetRb == null || targetNetObj == null || netBridge == null) return;

        if (interactor != null && interactor.cam != null)
        {
            Vector3 throwVelocity = lastGrabVel + interactor.cam.forward * throwBoost;
            netBridge.RPC_ThrowObject(targetNetObj, throwVelocity);
        }

        targetRb = null;
        targetNetObj = null;
    }

    void MoveGrabbedObject()
    {
        if (interactor == null || interactor.cam == null || targetRb == null) return;

        float desiredDist = grabHoldDistance;
        float actualDist = desiredDist;
        if (Physics.SphereCast(interactor.cam.position, grabbedRadius, interactor.cam.forward,
            out RaycastHit hit, desiredDist, grabBlock, QueryTriggerInteraction.Ignore))
        {
            actualDist = Mathf.Clamp(hit.distance - grabPadding, minHoldDistance, desiredDist);
        }

        Vector3 targetPos = interactor.cam.position + interactor.cam.forward * actualDist;
        Vector3 toTarget = targetPos - targetRb.position;
        Vector3 newPos = targetRb.position + toTarget * grabMoveSpeed * Time.fixedDeltaTime;

        lastGrabVel = (newPos - lastGrabPos) / Time.fixedDeltaTime;
        lastGrabPos = newPos;

        netBridge.RPC_MoveObject(targetNetObj, newPos);
    }
}