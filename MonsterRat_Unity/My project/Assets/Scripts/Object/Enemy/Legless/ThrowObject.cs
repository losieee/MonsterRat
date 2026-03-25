using Fusion;
using UnityEngine;

public class ThrowObject : NetworkBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float notifyCooldown = 0.5f;

    private float lastNotifyTime = -999f;

    private void OnCollisionEnter(Collision collision)
    {
        bool isGround = ((1 << collision.gameObject.layer) & groundLayer.value) != 0;

        if (!isGround)
            return;

        if (Time.time - lastNotifyTime < notifyCooldown)
        {
            return;
        }

        lastNotifyTime = Time.time;

        Vector3 impactPoint = collision.contacts.Length > 0
            ? collision.contacts[0].point
            : transform.position;

        MonsterLegless monster = FindAnyObjectByType<MonsterLegless>();

        if (monster == null)
        {
            return;
        }

        monster.RPC_InvestigatePoint(impactPoint);
    }
}
