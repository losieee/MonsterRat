using UnityEngine;

public class GasZone : MonoBehaviour
{
    public GasControl gas;
    public float checkRadius = 1.5f;      // 플레이어 주변 체크 반경
    public int minParticleCount = 30;     // 오염 증가 될 파티클 갯수
    public float exposurePerSec = 10f;    // 초당 오염 증가량
    public float checkInterval = 0.2f;    // 검사 주기

    float timer;

    private void Awake()
    {
        if (gas == null)
            gas = GetComponent<GasControl>();
    }

    // 가스 안에 있을 때
    private void OnTriggerStay(Collider other)
    {
        PlayerGas player = other.GetComponent<PlayerGas>();
        if (player == null) return;
        if (gas == null) return;

        // 검사 주기
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = checkInterval;

        // 주변 파티클 수 체크
        int nearCount = gas.CountParticlesNearWorldPos(player.transform.position, checkRadius);

        // 30개 이상이면 오염 증가
        if (nearCount >= minParticleCount)
        {
            player.AddExposure(exposurePerSec * checkInterval);
        }
    }
}
