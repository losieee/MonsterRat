using Fusion;
using UnityEngine;

public class PlayerGas : MonoBehaviour
{
    public float pollution = 0f;
    public float maxPollution = 100f;
    public float checkInterval = 0.2f;
    public float sampleHeightOffset = 0.8f;

    [Header("Mode")]
    public bool useNetworkAuthority = true;

    public float refreshZoneInterval = 1f;
    private float refreshTimer = 0f;

    private float timer = 0f;
    private NetworkObject networkObject;
    private GasZone[] gasZones;
    private PhotonGasMaskController gasMask;
    private PhotonNewGasMaskController newGasMask;

    private FullGaugeBlind fullGaugeBlind;
    private FullGaugeSlow fullGaugeSlow;
    private FullGaugeHeadShake fullHeadShake;

    private bool isEffectRunning = false;

    private void Awake()
    {
        networkObject = GetComponentInParent<NetworkObject>();
        gasMask = GetComponentInParent<PhotonGasMaskController>();
        newGasMask = GetComponentInParent<PhotonNewGasMaskController>();
        fullGaugeBlind = GetComponentInParent<FullGaugeBlind>();
        fullGaugeSlow = GetComponentInParent<FullGaugeSlow>();
        fullHeadShake = GetComponentInParent<FullGaugeHeadShake>();
    }

    private void Start()
    {
        RefreshGasZones();
    }

    private void Update()
    {
        if (!CanProcessGas())
            return;

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            RefreshGasZones();
            refreshTimer = refreshZoneInterval;
        }

        if (gasZones == null || gasZones.Length == 0)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = checkInterval;

        Vector3 pos = transform.position + Vector3.up * sampleHeightOffset;

        for (int i = 0; i < gasZones.Length; i++)
        {
            GasZone zone = gasZones[i];
            if (zone == null) continue;

            if (!zone.Contains(pos)) continue;
            if (!zone.IsDangerousAt(pos)) continue;

            if (gasMask != null && gasMask.UseMask)
                break;

            if (newGasMask != null && newGasMask.UseMask)
                break;

            AddExposure(zone.exposurePerSec * checkInterval);
            break;
        }

        CheckFullPollution();
    }

    // 게이지 다 찾는지 확인
    void CheckFullPollution()
    {
        if (isEffectRunning)
            return;

        if (pollution < maxPollution)
            return;

        isEffectRunning = true;

        int randomEffect = Random.Range(0, 3);
        // 0 = 실명
        // 1 = 둔화
        // 2 = 멀미

        if (randomEffect == 0)
        {
            if (fullGaugeBlind != null)
                fullGaugeBlind.StartBlind(OnEffectFinished);
            else
                OnEffectFinished();
        }
        else if (randomEffect == 1)
        {
            if (fullGaugeSlow != null)
                fullGaugeSlow.StartSlow(OnEffectFinished);
            else
                OnEffectFinished();
        }
        else
        {
            if (fullHeadShake != null)
                fullHeadShake.StartShake(OnEffectFinished);
            else
                OnEffectFinished();
        }
    }

    void OnEffectFinished()
    {
        pollution = 0f;
        isEffectRunning = false;
    }

    // 가스 확인 
    public void RefreshGasZones()
    {
        gasZones = FindObjectsOfType<GasZone>(true);
    }

    bool CanProcessGas()
    {
        if (!useNetworkAuthority)
            return true;

        return networkObject != null && networkObject.HasInputAuthority;
    }

    public void AddExposure(float amount)
    {
        pollution += amount;
        pollution = Mathf.Clamp(pollution, 0f, maxPollution);
    }

    public float GetNormalized()
    {
        if (maxPollution <= 0f) return 0f;
        return pollution / maxPollution;
    }
}