using UnityEngine;

public class PlayerRaycast : MonoBehaviour
{
    [Header("Raycast")]
    public Transform cam;
    public float distance = 3f;
    public LayerMask interactMask;      // 잡을 물체

    [Header("Aim Assist")]
    public bool useSphereCast = true;
    public float sphereRadius = 0.2f;

    [Header("Debug Gizmo")]
    public bool showGizmo = true;
    public Color rayColor = Color.red;
    public Color hitColor = Color.green;
    public float hitSphereRadius = 0.08f;

    // 스패너용 (바라보고있는 물체 자체에 있는 rigidbody 탐색)
    GameObject lookTarget;
    public GameObject LookTarget => lookTarget;

    // 쓰레기용 (바라보고있는 물체 부모에 있는 rigidbody 탐색)
    GameObject lookRigidTarget;
    public GameObject LookRigidTarget => lookRigidTarget;

    PlayerUIState ui;

    void Awake()
    {
        ui = GetComponent<PlayerUIState>();

        if (cam == null)
        {
            Camera c = GetComponentInChildren<Camera>();
            if (c != null) cam = c.transform;
        }
    }

    void Update()
    {
        if (ui != null && ui.IsUIOpen)
        {
            lookTarget = null;
            lookRigidTarget = null;
            return;
        }

        if (cam == null) return;

        Ray ray = new Ray(cam.position, cam.forward);

        RaycastHit hit;
        bool isHit;

        if (useSphereCast)
            isHit = Physics.SphereCast(ray, sphereRadius, out hit, distance, interactMask, QueryTriggerInteraction.Ignore);
        else
            isHit = Physics.Raycast(ray, out hit, distance, interactMask, QueryTriggerInteraction.Ignore);

        if (isHit)
        {
            // 스패너용 (본인한테 있는 Rigidbody)
            lookTarget = hit.collider.gameObject;

            // 쓰레기용 (부모에 있는 Rigidbody)
            Rigidbody rb = hit.collider.GetComponentInParent<Rigidbody>();
            lookRigidTarget = rb != null ? rb.gameObject : null;
        }
        else
        {
            lookTarget = null;
        }

        Debug.DrawRay(cam.position, cam.forward * distance, Color.yellow);
    }

    // 레이캐스트를 하고 싶을 때 사용하는 함수
    public bool RaycastWorld(float maxDist, out RaycastHit hit)
    {
        hit = default;
        if (cam == null) return false;

        Physics.SyncTransforms();

        Ray ray = new Ray(cam.position, cam.forward);
        return Physics.Raycast(ray, out hit, maxDist, ~0, QueryTriggerInteraction.Ignore);
    }

    // 쥐 조준 반경을 잡고 싶을 때 사용하는 함수
    public bool SphereCast(float radius, float maxDist, LayerMask mask, out RaycastHit hit)
    {
        hit = default;
        if (cam == null) return false;

        Physics.SyncTransforms();

        Ray ray = new Ray(cam.position, cam.forward);
        return Physics.SphereCast(ray, radius, out hit, maxDist, mask, QueryTriggerInteraction.Ignore);
    }

    // lookTarget 초기화
    public void ForceClearLookTarget()
    {
        lookTarget = null;
    }

    // lookTarget 물체로 지정
    public void ForceSetLookTarget(GameObject obj)
    {
        lookTarget = obj;
    }

    void OnDrawGizmos()
    {
        if (!showGizmo || cam == null) return;

        Ray ray = new Ray(cam.position, cam.forward);

        if (useSphereCast)
        {
            Gizmos.color = rayColor;
            Vector3 end = ray.origin + ray.direction * distance;

            Gizmos.DrawWireSphere(ray.origin, sphereRadius);
            Gizmos.DrawLine(ray.origin, end);
            Gizmos.DrawWireSphere(end, sphereRadius);

            if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, distance, interactMask, QueryTriggerInteraction.Ignore))
            {
                Gizmos.color = hitColor;
                Gizmos.DrawSphere(hit.point, hitSphereRadius);
                Gizmos.DrawLine(ray.origin, hit.point);
            }
        }
        else
        {
            Gizmos.color = rayColor;
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * distance);

            if (Physics.Raycast(ray, out RaycastHit hit, distance, interactMask, QueryTriggerInteraction.Ignore))
            {
                Gizmos.color = hitColor;
                Gizmos.DrawSphere(hit.point, hitSphereRadius);
                Gizmos.DrawLine(ray.origin, hit.point);
            }
        }
    }
}
