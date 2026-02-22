using Unity.VisualScripting;
using UnityEditor.UIElements;
using UnityEngine;

public class HandGrab : InvenBase
{
    public override ToolType Type => ToolType.Hand;

    [Header("Grab")]
    public float grabHoldDistance = 3f;
    public float grabMoveSpeed = 15f;
    public float throwBoost = 2.5f;
    public LayerMask grabBlock;
    public float grabPadding = 0.05f;       // 물체와 벽 사이 거리
    public float minHoldDistance = 0.6f;    // 플레이어와 물체 최소 거리

    Rigidbody targetRb;
    Vector3 lastGrabPos;
    Vector3 lastGrabVel;
    float grabbedRadius = 0.25f;

    public override void Tick()
    {
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

        if(Input.GetKeyDown(KeyCode.E))
        {
            Interaction();
        }
    }

    public override void FixedTick()
    {
        if (targetRb != null)
            MoveGrabbedObject();
    }

    void TryGrab(GameObject target)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null) return;

        targetRb = rb;
        targetRb.freezeRotation = true;
        targetRb.useGravity = false;

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
        if (targetRb == null) return;
        if (interactor == null || interactor.cam == null)
        {
            targetRb = null;
            return;
        }

        targetRb.freezeRotation = false;
        targetRb.useGravity = true;
        // 물체 던지기
        targetRb.velocity = lastGrabVel + interactor.cam.forward * throwBoost;
        targetRb = null;
    }

    void MoveGrabbedObject()
    {
        if (interactor == null || interactor.cam == null) return;

        float desiredDist = grabHoldDistance;
        float actualDist = desiredDist;

        if (Physics.SphereCast(interactor.cam.position, grabbedRadius, interactor.cam.forward,
            out RaycastHit hit, desiredDist, grabBlock, QueryTriggerInteraction.Ignore))
        {
            // 물체와의 거리 줄임
            actualDist = Mathf.Clamp(hit.distance - grabPadding, minHoldDistance, desiredDist);
        }

        Vector3 targetPos = interactor.cam.position + interactor.cam.forward * actualDist;

        // 현재 위치 -> 목표 위치로 보간 이동
        Vector3 toTarget = targetPos - targetRb.position;
        Vector3 newPos = targetRb.position + toTarget * grabMoveSpeed * Time.fixedDeltaTime;

        // 던지기 속도
        lastGrabVel = (newPos - lastGrabPos) / Time.fixedDeltaTime;
        lastGrabPos = newPos;
        targetRb.MovePosition(newPos);
    }

    void Interaction()
    {
        if(interactor.RaycastWorld(grabHoldDistance,out RaycastHit hit))
        {
            int layer = hit.collider.gameObject.layer;
            if (layer != 10 && layer != 17 && layer != 18) return;

            if (layer == 10)
            {
                DeleteObject btn = hit.collider.GetComponent<DeleteObject>();
                if (btn != null)
                {
                    btn.CanDelete();
                }
            }
            if (layer == 17)
            {
                SafeZone_Door btn = hit.collider.GetComponent<SafeZone_Door>();
                if(btn != null)
                {
                    btn.OpenDoor();
                }
            }
            if (layer == 18)
            {
                SafeZone_Door btn = hit.collider.GetComponentInParent<SafeZone_Door>();
                if (btn != null)
                {
                    btn.OpenClearDoor();
                }
            }
        }
    }
}
