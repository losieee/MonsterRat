using UnityEngine;
using Fusion;

public class ReClearPipe : NetworkBehaviour
{
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (RePollutionSpawner.Instance != null)
        {
            RePollutionSpawner.Instance.UnregisterGas(Object);
        }
    }
}
