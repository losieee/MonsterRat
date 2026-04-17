using UnityEngine;

public class TutorialGun : TutorialInvenBase
{
    public override TutorialToolType Type => TutorialToolType.Gun;

    [Header("Rat")]
    public float ratDistance = 8f;
    public float ratAimRadius = 0.15f;
    public LayerMask groundMask;
    public LayerMask ratMask;
    public LayerMask roachMask;
    public LayerMask targetMask;

    [Header("Prefabs")]
    public GameObject bloodPreb;
    public GameObject pollutionPreb;

    public override void Tick()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (TryShootRat()) return;
        if (TryShootRoach()) return;
        //if (TryShootTarget()) return;
        //TrySpawnPollutionAtHit();
    }

    // 바퀴 잡기
    bool TryShootRoach()
    {
        if (interactor == null) return false;

        if (interactor.SphereCast(ratAimRadius, ratDistance, roachMask, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;
            RoachController coach = hitObj.GetComponentInParent<RoachController>();
            GameObject CoachObj = coach != null ? coach.gameObject : hitObj.transform.root.gameObject;

            RoachController rc = CoachObj.GetComponent<RoachController>();
            if (rc != null) rc.enabled = false;

            if (bloodPreb != null)
            {
                Vector3 pos = CoachObj.transform.position;
                Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Instantiate(bloodPreb, pos, rot);
            }

            Rigidbody rb = CoachObj.GetComponentInChildren<Rigidbody>();
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.freezeRotation = false;
            SetLayerRecursively(CoachObj, 3);

            interactor.ForceSetLookTarget(CoachObj);
            return true;
        }

        return false;
    }

    // 쥐 잡기
    bool TryShootRat()
    {
        if (interactor == null) return false;

        if (interactor.SphereCast(ratAimRadius, ratDistance, ratMask, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;
            RatController rat = hitObj.GetComponentInParent<RatController>();
            GameObject ratObj = rat != null ? rat.gameObject : hitObj.transform.root.gameObject;

            RatController rc = ratObj.GetComponent<RatController>();
            if (rc != null)
            {
                if (!rc.enabled) return false;
                rc.enabled = false;
            }
            if (rc != null) rc.enabled = false;
            //ratObj.transform.GetChild(1).gameObject.SetActive(false);

            if (bloodPreb != null)
            {
                Vector3 start = hit.point + Vector3.up * 0.3f;
                if (Physics.Raycast(start, Vector3.down, out RaycastHit groundHit, 5f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    Vector3 pos = groundHit.point;
                    Quaternion rot = Quaternion.FromToRotation(Vector3.up, groundHit.normal);
                    Instantiate(bloodPreb, pos, rot);
                }
                else
                {
                    Instantiate(bloodPreb, hit.point, Quaternion.identity);
                }
            }

            Rigidbody rb = ratObj.GetComponentInChildren<Rigidbody>();
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.freezeRotation = false;

            SetLayerRecursively(ratObj, 3);

            var tm = Object.FindAnyObjectByType<TutorialManager>();
            if (tm != null) tm.NotifyRatKilled(ratObj);

            interactor.ForceSetLookTarget(ratObj);
            return true;
        }

        return false;
    }

    // 꽃, 좀비, 몬스터 등등
    /*bool TryShootTarget()
    {
        if (interactor == null) return false;

        if (interactor.SphereCast(ratAimRadius, ratDistance, targetMask, out RaycastHit hit))
        {
            ShootTarget target = hit.collider.GetComponentInParent<ShootTarget>();
            if (target == null) return false;

            target.ApplyHit();
            interactor.ForceSetLookTarget(target.gameObject);
            return true;
        }

        return false;
    }*/

    // 쥐 말고 다른곳 맞았을 때 프리팹 소환
    bool TrySpawnPollutionAtHit()
    {
        if (interactor == null) return false;
        if (pollutionPreb == null) return false;

        if (interactor.RaycastWorld(ratDistance, out RaycastHit hit))
        {
            int layer = hit.collider.gameObject.layer;

            if (layer == 3 || layer == 6 || layer == 8) return false;

            Vector3 pos = hit.point;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Instantiate(pollutionPreb, pos, rot);
            return true;
        }

        return false;
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        for (int i = 0; i < obj.transform.childCount; i++)
            SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
    }
}
