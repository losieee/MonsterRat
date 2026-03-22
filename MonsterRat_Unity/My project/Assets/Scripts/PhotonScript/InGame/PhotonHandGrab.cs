using UnityEngine;
using Fusion;

public class PhotonHandGrab : InvenBase
{
    public override ToolType Type => ToolType.Hand;

    [Header("Grab Settings")]
    public float grabHoldDistance = 4f;
    public float grabMoveSpeed = 15f;
    public float throwBoost = 2.5f;
    public LayerMask grabBlock;
    public float grabPadding = 0.05f;
    public float minHoldDistance = 0.6f;
    public LayerMask grabLayerMask = 1 << 3;

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
            GameObject t = null;

            if (Physics.Raycast(interactor.cam.position, interactor.cam.forward, out RaycastHit hit, grabHoldDistance, grabLayerMask))
            {
                t = hit.collider.gameObject;
            }
            else
            {
                Vector3 rayStart = interactor.cam.position - (interactor.cam.forward * 0.5f);
                if (Physics.SphereCast(rayStart, 0.5f, interactor.cam.forward, out RaycastHit sphereHit, grabHoldDistance + 0.5f, grabLayerMask))
                {
                    t = sphereHit.collider.gameObject;
                }
            }

            if (t != null)
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

        // 💡 [수정 포인트 1] 호스트의 권한을 기다리는 동안 바닥으로 떨어지지 않게 로컬에서 즉시 멈춤!
        targetRb.isKinematic = true;
        targetRb.useGravity = false;

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

            // 💡 놓을 때도 로컬에서 즉시 물리 활성화
            targetRb.isKinematic = false;
            targetRb.useGravity = true;

            netBridge.RPC_ReleaseAndThrow(targetNetObj, throwVelocity);
        }

        targetRb = null;
        targetNetObj = null;
    }

    void MoveGrabbedObject()
    {
        if (interactor == null || interactor.cam == null || targetRb == null || targetNetObj == null) return;

        // 원본에 있던 완벽한 권한 체크 로직 그대로 사용!
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

        // 💡 [수정 포인트 2] 마우스를 확 돌릴 때 속도가 무한대로 튀어 물리엔진이 폭발하는 것(Ghost Collider) 완벽 방지!
        Vector3 calcVel = (newPos - lastGrabPos) / Time.fixedDeltaTime;
        lastGrabVel = Vector3.ClampMagnitude(calcVel, 25f);

        lastGrabPos = newPos;
        virtualGrabPos = newPos;

        // 💡 [수정 포인트 3] 클라이언트가 서버를 기다리지 않고 내 화면의 빨간 상자(Rigidbody)를 즉시 움직임!
        // 이 한 줄 덕분에 클라이언트 홀드 위치가 이상해지거나 덜덜거리는 현상이 완전히 사라집니다.
        targetRb.MovePosition(newPos);

        if (!targetNetObj.HasStateAuthority)
        {
            netBridge.RPC_MoveObjectUnreliable(targetNetObj, newPos);
        }
    }
}