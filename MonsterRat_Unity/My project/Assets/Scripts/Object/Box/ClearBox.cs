using UnityEngine;

public class ClearBox : MonoBehaviour, IClearTarget
{
    public float Remain01 => 1f;

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
