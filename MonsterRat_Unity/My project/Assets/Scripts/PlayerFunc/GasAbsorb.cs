using UnityEngine;

public class GasAbsorb : MonoBehaviour
{
    [Header("Ref")]
    public Transform cam;
    public Transform nozzle;                // 빨아들일 위치
    public ParticleSystem vacuumFx;         // 빨아들이는 이펙트 (나중에 추가 예정)

    [Header("Suck")]
    public float range = 8f;
    public float aimRadius = 0.3f;
    public float suckRadius = 1.2f;         // 가스 흡입 범위
    public float aimAngle = 25f;            // 정면 각도
    public float removePerSecond = 30f;     // 흡입 속도 (1초당 삭제)
    public LayerMask gasMask;

    void LateUpdate()
    {
        if (cam != null && nozzle != null)
            nozzle.rotation = cam.rotation;
    }

    public void SuckTick(float dt)
    {
        if (cam == null || nozzle == null) return;

        Ray ray = new Ray(cam.position, cam.forward);

        bool hitGas = Physics.SphereCast(ray, aimRadius, out RaycastHit hit, range, gasMask, QueryTriggerInteraction.Collide);

        GasControl gas = null;

        // 앞에있는 가스 찾기
        if (hitGas)
        {
            gas = hit.collider.GetComponentInParent<GasControl>();
        }
        else
        {
            // 앞에 없으면 노즐 주변 가스들 중 정면만 찾기
            Collider[] cols = Physics.OverlapSphere(nozzle.position, suckRadius, gasMask, QueryTriggerInteraction.Collide);

            if (cols.Length > 0)
            {
                float bestDot = -1f;
                float cosLimit = Mathf.Cos(aimAngle * Mathf.Deg2Rad);

                for (int i = 0; i < cols.Length; i++)
                {
                    // 카메라 기준 가장 가까운 표면 지점
                    Vector3 closest = cols[i].ClosestPoint(cam.position);
                    Vector3 dir = (closest - cam.position).normalized;

                    // 정면 판정
                    float dot = Vector3.Dot(cam.forward, dir);

                    // 앞에있는것만 선택
                    if (dot >= cosLimit && dot > bestDot)
                    {
                        bestDot = dot;
                        gas = cols[i].GetComponentInParent<GasControl>();
                    }
                }
            }
        }

        // 가스가 없으면 종료
        if (gas == null)
        {
            StopFx();
            return;
        }

        // 삭제할 파티클 수 계산
        int removeCount = Mathf.CeilToInt(removePerSecond * Time.deltaTime);

        // 가스 흡입
        gas.Suck(nozzle.position, cam.forward, suckRadius, removeCount, dt, aimAngle);
        //PlayFx();

        // 가스 없으면 오브젝트 삭제
        if (gas.IsEmpty())
            Destroy(gas.gameObject);
    }

    // 우클릭 끝났을 때 호출할 함수
    public void StopSuck()
    {
        StopFx();
    }

    // 빨아들이는 파티클 (예:가스 흡입기 입구쪽 소용돌이)
    void PlayFx()
    {
        if (vacuumFx != null && !vacuumFx.isPlaying)
            vacuumFx.Play();
    }

    // 위에꺼 중단
    void StopFx()
    {
        if (vacuumFx != null && vacuumFx.isPlaying)
            vacuumFx.Stop();
    }
}
