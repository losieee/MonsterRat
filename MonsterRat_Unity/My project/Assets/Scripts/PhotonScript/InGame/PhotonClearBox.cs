using UnityEngine;
using Fusion;
public class PhotonClearBox : NetworkBehaviour, IClearTarget
{
    public float weight = 1f;
    public float Remain01 => 1f;
    public float Weight => Mathf.Max(0f, weight);

    void Start()
    {
        if (ClearManager.Instance != null)
            ClearManager.Instance.Register(this);
    }

    void OnDestroy()
    {
        if (ClearManager.Instance != null)
            ClearManager.Instance.Unregister(this);
    }
}
