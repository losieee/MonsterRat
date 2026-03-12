using UnityEngine;

public class TutorialHandGrab : TutorialInvenBase
{
    public override TutorialToolType Type => TutorialToolType.Hand;

    [Header("Grab")]
    public float grabHoldDistance = 3f;
    public float grabMoveSpeed = 15f;
    public float throwBoost = 2.5f;
    public LayerMask grabBlock;
    public float grabPadding = 0.05f;
    public float minHoldDistance = 0.6f;

    TutorialInventory inven;
    Rigidbody targetRb;
    Vector3 lastGrabPos;
    Vector3 lastGrabVel;
    float grabbedRadius = 0.25f;

    private void Awake()
    {
        inven = GetComponent<TutorialInventory>();
    }

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

        if (Input.GetKeyDown(KeyCode.E))
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
        if (col == null)
            col = targetRb.GetComponentInChildren<Collider>();
        
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

        targetRb.linearVelocity = lastGrabVel + interactor.cam.forward * throwBoost;
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
            actualDist = Mathf.Clamp(hit.distance - grabPadding, minHoldDistance, desiredDist);
        }

        Vector3 targetPos = interactor.cam.position + interactor.cam.forward * actualDist;

        Vector3 toTarget = targetPos - targetRb.position;
        Vector3 newPos = targetRb.position + toTarget * grabMoveSpeed * Time.fixedDeltaTime;

        lastGrabVel = (newPos - lastGrabPos) / Time.fixedDeltaTime;
        lastGrabPos = newPos;
        targetRb.MovePosition(newPos);
    }

    void Interaction()
    {
        if (!interactor.RaycastWorld(grabHoldDistance, out RaycastHit hit)) return;

        int layer = hit.collider.gameObject.layer;

        switch (layer)
        {
            case 10:
                {
                    DeleteObject btn = hit.collider.GetComponent<DeleteObject>();
                    if (btn != null) btn.CanDelete();
                    break;
                }
            case 17:
                {
                    SafeZone_Door btn = hit.collider.GetComponentInParent<SafeZone_Door>();
                    if (btn != null) btn.OpenDoor();
                    break;
                }
            case 18:
                {
                    SafeZone_Door btn = hit.collider.GetComponentInParent<SafeZone_Door>();
                    if (btn != null) btn.OpenClearDoor();
                    break;
                }
            case 20:
                {
                    Destroy(hit.collider.gameObject);
                    if (inven == null) return;

                    inven.AddTool(TutorialToolType.Mop);
                    inven.hasMop = true;
                    break;
                }
            case 21:
                {
                    Destroy(hit.collider.gameObject);
                    if (inven == null) return;

                    inven.AddTool(TutorialToolType.Gun);
                    inven.hasGun = true;
                    break;
                }
            case 22:
                {
                    Destroy(hit.collider.gameObject);
                    if (inven == null) return;

                    inven.AddTool(TutorialToolType.Spanner);
                    inven.hasSpanner = true;
                    break;
                }
            default:
                // ¹«½Ã
                break;
        }
    }
}
