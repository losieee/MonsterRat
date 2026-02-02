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

        // 반경 안 가스 콜라이더 전부
        Collider[] cols = Physics.OverlapSphere(nozzle.position, suckRadius, gasMask, QueryTriggerInteraction.Collide);

        if (cols == null || cols.Length == 0)
        {
            StopFx();
            return;
        }

        // 중복 제거 (같은 GasControl 콜라이더 제외)
        var gasSet = new System.Collections.Generic.HashSet<GasControl>();

        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null) continue;
            GasControl gas = cols[i].GetComponentInParent<GasControl>();
            if (gas != null) gasSet.Add(gas);
        }

        if (gasSet.Count == 0)
        {
            StopFx();
            return;
        }

        // 전체 제거량을 가스 개수만큼 나누기
        int totalRemove = Mathf.CeilToInt(removePerSecond * dt);
        int perGasRemove = Mathf.Max(1, Mathf.CeilToInt((float)totalRemove / gasSet.Count));

        bool suckedSomething = false;

        // 반경 안에 있는 모든 가스 흡입
        foreach (var gas in gasSet)
        {
            if (gas == null) continue;

            gas.Suck(nozzle.position, cam.forward, suckRadius, perGasRemove, dt, aimAngle);
            suckedSomething = true;

            if (gas.IsEmpty())
                Destroy(gas.gameObject);
        }

        if (!suckedSomething)
            StopFx();
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
