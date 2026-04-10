using UnityEngine;
using Fusion;

public class StandaloneHandGrab : NetworkBehaviour
{
    [Header("Grab Settings")]
    public float grabRange = 10f;
    public float holdDistance = 2f;
    public float followSpeed = 10f;
    public float maxFollowVelocity = 8f;
    public float stopDistance = 0.04f;

    [Header("Release Settings")]
    public float throwForce = 6f;
    public float minReleaseVelocity = 0.5f;

    [Header("Collision Settings")]
    public LayerMask interactLayer;
    public LayerMask grabBlock;
    public float grabPadding = 0.05f;
    public float minHoldDistance = 0.6f;
    public float grabDetectRadius = 0.2f;

    [Header("References")]
    public Transform aimTransform;
    public PlayerRaycast playerRaycast;

    // 현재 잡고있는 네트워크 오브젝트
    [Networked] public NetworkObject GrabbedObject { get; set; }
    // 게스트가 보는 위치,방향을 호스트에 넘겨서 게스트의 실제 조준하는 방향으로 물체를 움직일 수 있게 하기 위한 값 
    [Networked] private Vector3 NetAimPosition { get; set; }    
    [Networked] private Vector3 NetAimForward { get; set; }

    private Rigidbody grabbedRb;
    private float grabbedRadius = 0.25f;

    private Vector3 lastAimPos;
    private Vector3 handVel;
    private Vector3 smoothedHandVel;

    // 물체를 들고있는 상태인지 확인
    private bool IsHolding => GrabbedObject != null && grabbedRb != null;

    private void Awake()
    {
        if (playerRaycast == null)
            playerRaycast = GetComponent<PlayerRaycast>();
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        // 내가 조준하고 있는 방향을 호스트한테 계속해서 보냄
        if (aimTransform != null)
        {
            Rpc_UpdateAim(aimTransform.position, aimTransform.forward);
        }

        if (Input.GetMouseButtonDown(1))
        {
            TryGrabLocal();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            if (GrabbedObject != null)
            {
                Vector3 releaseVel = smoothedHandVel;

                // 물건 움직임이 적으면 덜덜 떨리지 말고 그냥 멈춤
                if (releaseVel.magnitude < minReleaseVelocity)
                    releaseVel = Vector3.zero;

                // 던지는 느낌 추가
                releaseVel += aimTransform.forward * throwForce;

                Rpc_ReleaseObject(releaseVel);
            }
        }
    }

    private void TryGrabLocal()
    {
        if (GrabbedObject != null) return;

        NetworkObject netObj = null;

        // PlayerRaycast에서 이미 찾은 대상을 사용
        if (playerRaycast != null && playerRaycast.LookTarget != null)
        {
            netObj = playerRaycast.LookRigidTarget.GetComponentInParent<NetworkObject>();
        }

        // 직접 SphereCast
        if (netObj == null)
        {
            Transform origin = playerRaycast != null && playerRaycast.cam != null
                ? playerRaycast.cam
                : aimTransform;

            if (origin == null) return;

            Ray ray = new Ray(origin.position, origin.forward);

            if (Physics.SphereCast(ray, grabDetectRadius, out RaycastHit hit, grabRange, interactLayer, QueryTriggerInteraction.Ignore))
            {
                netObj = hit.collider.GetComponentInParent<NetworkObject>();
            }
        }

        if (netObj != null)
        {
            Rpc_TryGrabObject(netObj);
        }
    }

    // 내가 조준하고 있는 방향을 호스트에게 전달
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Unreliable)]
    private void Rpc_UpdateAim(Vector3 aimPos, Vector3 aimForward)
    {
        NetAimPosition = aimPos;
        NetAimForward = aimForward;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_TryGrabObject(NetworkObject targetObj)
    {
        if (targetObj == null) return;

        GrabbedObject = targetObj;
        grabbedRb = targetObj.GetComponent<Rigidbody>();

        if (grabbedRb == null)
        {
            GrabbedObject = null;
            return;
        }

        grabbedRb.WakeUp();
        grabbedRb.isKinematic = false;
        grabbedRb.useGravity = false;
        grabbedRb.freezeRotation = true;
        grabbedRb.linearDamping = 12f;
        grabbedRb.angularDamping = 10f;
        grabbedRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        grabbedRb.interpolation = RigidbodyInterpolation.Interpolate;
        grabbedRb.linearVelocity = Vector3.zero;
        grabbedRb.angularVelocity = Vector3.zero;

        Collider col = grabbedRb.GetComponent<Collider>();
        if (col == null)
            col = grabbedRb.GetComponentInChildren<Collider>();

        if (col != null)
        {
            Vector3 e = col.bounds.extents;
            grabbedRadius = Mathf.Max(e.x, e.y, e.z);
        }
        else
        {
            grabbedRadius = 0.25f;
        }

        if (aimTransform != null)
        {
            NetAimPosition = aimTransform.position;
            NetAimForward = aimTransform.forward;
            lastAimPos = aimTransform.position;
            handVel = Vector3.zero;
            smoothedHandVel = Vector3.zero;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_ReleaseObject(Vector3 releaseVelocity)
    {
        if (GrabbedObject == null) return;

        Rigidbody rb = GrabbedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = false;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.linearVelocity = Vector3.ClampMagnitude(releaseVelocity, 12f);
            rb.WakeUp();
        }

        grabbedRb = null;
        GrabbedObject = null;
        handVel = Vector3.zero;
        smoothedHandVel = Vector3.zero;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !IsHolding || aimTransform == null)
            return;

        MoveGrabbedObject();
    }

    private void MoveGrabbedObject()
    {
        float desiredDist = holdDistance;
        float actualDist = desiredDist;

        // 네트워크로 받은 값 (aimPos, aimForward) 사용
        // 이걸로 게스트도 위아래 시선처리 가능
        Vector3 aimPos = NetAimPosition;
        Vector3 aimForward = NetAimForward.sqrMagnitude > 0.0001f ? NetAimForward.normalized : transform.forward;

        // 물체와 손 사이에 벽이 있으면 벽 앞까지 오게 함 (벽에 막히는 느낌)
        if (Physics.SphereCast(
                aimPos,
                grabbedRadius,
                aimForward,
                out RaycastHit hit,
                desiredDist,
                grabBlock,
                QueryTriggerInteraction.Ignore))
        {
            float blockedDist = hit.distance - grabPadding;
            // 앞이 아예 막혔을 때만 거리 줄이기
            if (blockedDist < desiredDist - 0.15f)
            {
                actualDist = Mathf.Clamp(blockedDist, minHoldDistance, desiredDist);
            }
        }

        Vector3 targetPos = aimPos + aimForward * actualDist;
        Vector3 toTarget = targetPos - grabbedRb.position;

        // 손 이동 속도 계산
        handVel = (aimPos - lastAimPos) / Runner.DeltaTime;
        lastAimPos = aimPos;

        // 릴리즈용 속도는 부드럽게 평균화
        smoothedHandVel = Vector3.Lerp(smoothedHandVel, handVel, 0.35f);

        // 목표지점에 거의 다 왔으면 속도 0
        // 바들바들 떨림 방지
        if (toTarget.sqrMagnitude < stopDistance * stopDistance)
        {
            grabbedRb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 desiredVel = toTarget * followSpeed;
        desiredVel = Vector3.ClampMagnitude(desiredVel, maxFollowVelocity);

        grabbedRb.linearVelocity = desiredVel;
    }
}