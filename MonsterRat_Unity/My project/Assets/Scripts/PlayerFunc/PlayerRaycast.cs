using UnityEngine;

public class PlayerRaycast : MonoBehaviour
{
    [Header("Raycast")]
    public Transform cam;
    public float distance = 3f;
    public LayerMask interactMask;      // 잡을 물체

    GameObject lookTarget;
    public GameObject LookTarget => lookTarget;

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
            return;
        }

        if (cam == null) return;
        Ray ray = new Ray(cam.position, cam.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, distance, interactMask, QueryTriggerInteraction.Ignore))
            lookTarget = hit.collider.gameObject;
        else
            lookTarget = null;
    }

    // 도구가 "월드 전체(~0)" 대상으로 레이캐스트를 하고 싶을 때 사용하는 함수
    public bool RaycastWorld(float maxDist, out RaycastHit hit)
    {
        hit = default;
        if (cam == null) return false;

        Ray ray = new Ray(cam.position, cam.forward);
        return Physics.Raycast(ray, out hit, maxDist, ~0, QueryTriggerInteraction.Ignore);
    }

    // 도구가 SphereCast(예: 쥐 조준 반경)를 하고 싶을 때 사용하는 함수
    public bool SphereCast(float radius, float maxDist, LayerMask mask, out RaycastHit hit)
    {
        hit = default;
        if (cam == null) return false;

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
}
