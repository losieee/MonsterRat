using Fusion;
using UnityEngine;

public class PlayerGas : NetworkBehaviour
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

    private bool shakeActive = false;
    private bool blindActive = false;
    private bool slowActive = false;

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

        if (gasZones != null && gasZones.Length > 0)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
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
            }
        }

        CheckPollutionStages();
    }

    private void CheckPollutionStages()
    {
        float normalized = GetNormalized();

        // 50% 이상: 멀미
        if (normalized >= 0.5f)
        {
            if (!shakeActive)
            {
                shakeActive = true;

                if (fullHeadShake != null)
                    fullHeadShake.StartShake(null);
            }
        }
        else
        {
            if (shakeActive)
            {
                shakeActive = false;

                if (fullHeadShake != null)
                    fullHeadShake.StopShake();
            }
        }

        // 75% 이상: 실명
        if (normalized >= 0.75f)
        {
            if (!blindActive)
            {
                blindActive = true;

                if (fullGaugeBlind != null)
                    fullGaugeBlind.StartBlind(null);
            }
        }
        else
        {
            if (blindActive)
            {
                blindActive = false;

                if (fullGaugeBlind != null)
                    fullGaugeBlind.StopBlind();
            }
        }

        // 100% 이상: 둔화
        if (normalized >= 1f)
        {
            if (!slowActive)
            {
                slowActive = true;

                if (fullGaugeSlow != null)
                    fullGaugeSlow.StartSlow(null);
            }
        }
        else
        {
            if (slowActive)
            {
                slowActive = false;

                if (fullGaugeSlow != null)
                    fullGaugeSlow.StopSlow();
            }
        }
    }

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

    [Rpc(RpcSources.All, RpcTargets.InputAuthority)]
    public void RPC_AddExposure(float amount)
    {
        AddExposure(amount);
    }
}