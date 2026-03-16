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

        // 내가 잡은 타겟이 있다면 방장에게 계속해서 이동 명령을 내립니다!
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

        // 1. 권한 요청 코드를 삭제하고, 무전기를 통해 방장에게 "중력 꺼주세요!" 라고만 요청합니다.
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
            // 2. 방장에게 "이 방향으로 던져주세요!" 라고 요청합니다.
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

        // 3. 방장에게 "제가 계산한 좌표로 상자 좀 옮겨주세요!" 실시간 요청
        netBridge.RPC_MoveObject(targetNetObj, newPos);
    }
}