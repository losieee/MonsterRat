using UnityEngine;

public class GasControl : MonoBehaviour
{
    public int initialParticles = 800;      // 처음 시작할 때 채워둘 양
    public float pullStr = 6f;              // 빨려오는 힘
    public float destroyDis = 0.2f;         // 삭제될 거리
    public float maxMovePerFrame = 0.05f;   // 최대

    ParticleSystem ps;
    ParticleSystem.Particle[] buffer;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();

        int max = ps.main.maxParticles;
        if (max < initialParticles) max = initialParticles;
        buffer = new ParticleSystem.Particle[max];
    }

    void Start()
    {
        ps.Clear();
        ps.Emit(initialParticles);
        ps.Play();
    }

    // 플레이어 주변에 파티클이 몇개 있는지 세는 함수
    public int CountParticlesNearWorldPos(Vector3 worldPos, float radius)
    {
        int alive = ps.GetParticles(buffer);
        if (alive <= 0) return 0;

        float r2 = radius * radius;

        var main = ps.main;
        bool isLocal = (main.simulationSpace == ParticleSystemSimulationSpace.Local);

        int count = 0;

        for (int i = 0; i < alive; i++)
        {
            // 파티클 위치를 월드 좌표로 통일
            Vector3 pWorld = isLocal ? transform.TransformPoint(buffer[i].position) : buffer[i].position;

            // 플레이어와 거리 체크
            Vector3 diff = pWorld - worldPos;
            if (diff.sqrMagnitude <= r2)
                count++;
        }

        return count;
    }

    public void Suck(Vector3 nozzleWorldPos, Vector3 nozzleForward, float radius, int removeCount, float dt, float aimAngle)
    {
        int alive = ps.GetParticles(buffer);
        if (alive <= 0) return;

        var main = ps.main;
        bool isLocal = (main.simulationSpace == ParticleSystemSimulationSpace.Local);

        // 파티클 위치 모드 변환
        Vector3 nozzlePos = isLocal ? transform.InverseTransformPoint(nozzleWorldPos) : nozzleWorldPos;

        float r2 = radius * radius;
        int removed = 0;

        // 정면 판정 기준 변환
        float dotLimit = Mathf.Cos(aimAngle * Mathf.Deg2Rad);

        Vector3 forward = nozzleForward.normalized;
        if (forward.sqrMagnitude < 0.0001f) return;

        for (int i = 0; i < alive; i++)
        {
            Vector3 pPos = buffer[i].position;

            // 흡입 범위 판정
            Vector3 toNozzle = nozzlePos - pPos;
            if (toNozzle.sqrMagnitude > r2)
                continue;

            // 파티클을 월드 좌표로 변환
            Vector3 pWorld = isLocal ? transform.TransformPoint(pPos) : pPos;
            Vector3 toParticle = (pWorld - nozzleWorldPos).normalized;

            float dot = Vector3.Dot(forward, toParticle);

            // 정면 아니면 흡입 금지
            if (dot < dotLimit)
                continue;

            float dist = toNozzle.magnitude;
            if (dist < 0.0001f) continue;

            Vector3 dir = toNozzle / dist;

            float move = Mathf.Min(maxMovePerFrame, pullStr * dt);

            // 파티클 이동
            buffer[i].position += dir * move;

            // 너무 가까우면 제거
            if (dist <= destroyDis && removed < removeCount)
            {
                buffer[i].remainingLifetime = 0f;
                removed++;
            }
        }

        ps.SetParticles(buffer, alive);
    }


    public bool IsEmpty()
    {
        return ps.particleCount <= 0;
    }

    public void SetEmission(bool on)
    {
        var em = ps.emission;
        em.enabled = on;
    }
}
