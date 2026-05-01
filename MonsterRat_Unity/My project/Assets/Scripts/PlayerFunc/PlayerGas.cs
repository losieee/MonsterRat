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

    private void Awake()
    {
        networkObject = GetComponentInParent<NetworkObject>();
        gasMask = GetComponentInParent<PhotonGasMaskController>();
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

            AddExposure(zone.exposurePerSec * checkInterval);
            break;
        }
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