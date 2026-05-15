using UnityEngine;
using Fusion;

public class ReClearBox : NetworkBehaviour
{
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (RePollutionSpawner.Instance != null)
        {
            RePollutionSpawner.Instance.UnregisterTrash(Object);
        }
    }
}