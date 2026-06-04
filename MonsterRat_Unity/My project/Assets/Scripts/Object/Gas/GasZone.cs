using UnityEngine;

public class GasZone : MonoBehaviour
{
    public GasControl gas;
    public float checkRadius = 1.5f;
    public int minParticleCount = 30;
    public float exposurePerSec = 10f;

    private void Awake()
    {
        if (gas == null)
            gas = GetComponent<GasControl>();
    }

    public bool Contains(Vector3 worldPos)
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return false;

        Vector3 localPos = transform.InverseTransformPoint(worldPos) - box.center;
        Vector3 halfSize = box.size * 0.5f;

        return Mathf.Abs(localPos.x) <= halfSize.x &&
               Mathf.Abs(localPos.y) <= halfSize.y &&
               Mathf.Abs(localPos.z) <= halfSize.z;
    }

    public bool IsDangerousAt(Vector3 worldPos)
    {
        if (gas == null) return false;

        int nearCount = gas.CountParticlesNearWorldPos(worldPos, checkRadius);
        return nearCount >= minParticleCount;
    }
}