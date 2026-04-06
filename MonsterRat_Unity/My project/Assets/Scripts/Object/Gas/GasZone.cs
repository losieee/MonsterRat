using UnityEngine;

public class GasZone : MonoBehaviour
{
    public GasControl gas;
    public float checkRadius = 1.5f;
    public int minParticleCount = 30;
    public float exposurePerSec = 10f;

    private Collider zoneCollider;

    private void Awake()
    {
        if (gas == null)
            gas = GetComponent<GasControl>();

        zoneCollider = GetComponent<Collider>();
    }

    public bool Contains(Vector3 worldPos)
    {
        if (zoneCollider == null) return false;

        Vector3 closest = zoneCollider.ClosestPoint(worldPos);
        float dist = Vector3.Distance(closest, worldPos);

        return dist < 0.05f;
    }

    public bool IsDangerousAt(Vector3 worldPos)
    {
        if (gas == null) return false;

        int nearCount = gas.CountParticlesNearWorldPos(worldPos, checkRadius);
        return nearCount >= minParticleCount;
    }
}