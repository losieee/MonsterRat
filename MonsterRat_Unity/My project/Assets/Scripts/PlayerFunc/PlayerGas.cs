using System;
using Fusion;
using UnityEngine;

public class PlayerGas : NetworkBehaviour
{
    [Networked] public float pollution { get; set; }
    [Networked] public float NetworkSpeedMultiplier { get; set; }

    [SerializeField] private float localPollution;      // 튜토리얼 전용

    public float maxPollution = 100f;
    public float checkInterval = 0.2f;
    public float sampleHeightOffset = 0.8f;

    [Header("Mode")]
    public bool useNetworkAuthority = true;

    public float refreshZoneInterval = 1f;
    private float refreshTimer = 0f;

    public float slowMoveMultiplier = 0.2f;

    private float localSpeedMultiplier = 1f;

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

    private bool IsNetworkReady
    {
        get
        {
            return Object != null && Object.IsValid;
        }
    }

    public bool IsReadyForGauge
    {
        get
        {
            if (!useNetworkAuthority)
                return true;

            return IsNetworkReady && HasInputAuthority;
        }
    }

    private bool CanApplyLocalEffect
    {
        get
        {
            if (!useNetworkAuthority)
                return true;

            return IsNetworkReady && HasInputAuthority;
        }
    }

    private void Awake()
    {
        gasMask = GetComponentInParent<PhotonGasMaskController>();
        newGasMask = GetComponentInParent<PhotonNewGasMaskController>();
        fullGaugeBlind = GetComponentInParent<FullGaugeBlind>();
        fullGaugeSlow = GetComponentInParent<FullGaugeSlow>();
        fullHeadShake = GetComponentInParent<FullGaugeHeadShake>();
    }

    public override void Spawned()
    {
        networkObject = Object;

        if (HasStateAuthority)
            NetworkSpeedMultiplier = 1f;

        RefreshGasZones();
    }

    private void Start()
    {
        if (!useNetworkAuthority)
            RefreshGasZones();
    }

    private void Update()
    {
        if (useNetworkAuthority)
        {
            if (!IsNetworkReady)
                return;

            if (!HasInputAuthority)
                return;
        }

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

                    ApplyExposure(zone.exposurePerSec * checkInterval);
                    break;
                }
            }
        }

        CheckPollutionStages();
    }

    private void ApplyExposure(float amount)
    {
        if (!useNetworkAuthority)
        {
            AddLocalExposure(amount);
            return;
        }

        if (!IsNetworkReady)
            return;

        if (HasStateAuthority)
        {
            AddNetworkExposure(amount);
        }
        else if (HasInputAuthority)
        {
            RPC_RequestAddExposure(amount);
        }
    }

    public float GetMoveSpeedMultiplier()
    {
        if (!useNetworkAuthority)
            return localSpeedMultiplier;

        if (!IsNetworkReady)
            return 1f;

        if (NetworkSpeedMultiplier <= 0f)
            return 1f;

        return NetworkSpeedMultiplier;
    }

    private void CheckPollutionStages()
    {
        if (!CanApplyLocalEffect)
            return;

        ApplyPollutionStages(GetNormalized());
    }

    private void ApplyPollutionStages(float normalized)
    {
        if (useNetworkAuthority)
        {
            if (!IsNetworkReady)
                return;

            if (!HasInputAuthority)
                return;
        }

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
        if (normalized >= 0.99f)
        {
            if (!slowActive)
            {
                slowActive = true;
            }
        }
        else
        {
            if (slowActive)
            {
                slowActive = false;
            }
        }
    }

    public void RefreshGasZones()
    {
        gasZones = FindObjectsOfType<GasZone>(true);
    }

    public void AddExposure(float amount)
    {
        if (!useNetworkAuthority)
        {
            AddLocalExposure(amount);
            return;
        }

        if (!IsNetworkReady)
            return;

        if (HasStateAuthority)
        {
            AddNetworkExposure(amount);
        }
        else if (HasInputAuthority)
        {
            RPC_RequestAddExposure(amount);
        }
    }

    private void AddLocalExposure(float amount)
    {
        localPollution += amount;
        localPollution = Mathf.Clamp(localPollution, 0f, maxPollution);

        float normalized = maxPollution <= 0f ? 0f : localPollution / maxPollution;

        localSpeedMultiplier = normalized >= 0.99f ? slowMoveMultiplier : 1f;

        ApplyPollutionStages(normalized);

    }

    private void AddNetworkExposure(float amount)
    {
        pollution += amount;
        pollution = Mathf.Clamp(pollution, 0f, maxPollution);

        float normalized = maxPollution <= 0f ? 0f : pollution / maxPollution;

        if (HasStateAuthority)
        {
            NetworkSpeedMultiplier = normalized >= 0.99f ? slowMoveMultiplier : 1f;
        }

        if (HasInputAuthority)
        {
            ApplyPollutionStages(normalized);
        }
        else
        {
            RPC_ApplyPollutionStagesOnOwner(pollution);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ApplyPollutionStagesOnOwner(float serverPollution)
    {
        float normalized = maxPollution <= 0f ? 0f : serverPollution / maxPollution;
        ApplyPollutionStages(normalized);
    }

    public float GetNormalized()
    {
        if (maxPollution <= 0f)
            return 0f;

        if (useNetworkAuthority && IsNetworkReady)
            return pollution / maxPollution;

        return localPollution / maxPollution;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestAddExposure(float amount)
    {
        AddNetworkExposure(amount);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddExposure(float amount)
    {
        AddNetworkExposure(amount);
    }

    public void RequestApplyExposureToTarget(NetworkObject targetObj, float amount)
    {
        if (targetObj == null) return;

        if (!useNetworkAuthority)
        {
            ApplyExposureToTarget(targetObj, amount);
            return;
        }

        if (!IsNetworkReady) return;

        // 호스트는 바로 처리
        if (HasStateAuthority)
        {
            ApplyExposureToTarget(targetObj, amount);
        }
        // 게스트는 Host에게 요청
        else
        {
            RPC_RequestApplyExposureToTarget(targetObj, amount);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestApplyExposureToTarget(NetworkObject targetObj, float amount)
    {
        ApplyExposureToTarget(targetObj, amount);
    }

    private void ApplyExposureToTarget(NetworkObject targetObj, float amount)
    {
        if (targetObj == null)
            return;

        PlayerGas targetGas = targetObj.GetComponent<PlayerGas>();

        if (targetGas == null)
            targetGas = targetObj.GetComponentInChildren<PlayerGas>();

        if (targetGas == null)
            return;

        if (targetGas.useNetworkAuthority)
        {
            targetGas.AddNetworkExposureFromServer(amount);

            if (amount < 0f)
            {
                targetGas.RPC_PlayAntidoteEffectOnOwner();
            }
        }
        else
        {
            targetGas.AddLocalExposureFromServer(amount);

            if (amount < 0f)
            {
                targetGas.PlayLocalAntidoteEffect();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_PlayAntidoteEffect()
    {
        if (GasGauge.Instance != null)
            GasGauge.Instance.PlayAntidoteEffect();
    }

    private void AddNetworkExposureFromServer(float amount)
    {
        AddNetworkExposure(amount);
    }

    private void AddLocalExposureFromServer(float amount)
    {
        AddLocalExposure(amount);
    }

    public static event Action OnLocalAntidoteEffectRequested;

    public void PlayLocalAntidoteEffect()
    {
        OnLocalAntidoteEffectRequested?.Invoke();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_PlayAntidoteEffectOnOwner()
    {
        OnLocalAntidoteEffectRequested?.Invoke();
    }
}