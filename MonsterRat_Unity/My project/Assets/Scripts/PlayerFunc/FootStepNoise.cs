using Fusion;
using UnityEngine;


public class FootStepNoise : NetworkBehaviour
{
    [SerializeField] private float soundInterval = 0.5f;
    [SerializeField] private float soundRadius = 12f;

    private float timer;

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority)
            return;

        if (!GetInput<MyNetworkInput>(out var input))
            return;

        timer -= Runner.DeltaTime;
        if (timer > 0f)
            return;

        if (!input.isRunning)
            return;

        timer = soundInterval;

        Collider[] hits = Physics.OverlapSphere(transform.position, soundRadius);

        foreach (Collider hit in hits)
        {
            MonsterLegless monster = hit.GetComponentInParent<MonsterLegless>();
            if (monster != null)
            {
                monster.HearRunningSound(transform.position);
            }
        }
    }
}
