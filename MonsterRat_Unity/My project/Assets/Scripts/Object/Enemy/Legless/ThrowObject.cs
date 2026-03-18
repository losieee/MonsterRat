using Mirror.Examples.CCU;
using UnityEngine;

public class ThrowObject : MonoBehaviour
{
    [SerializeField] private float notifyCooldown = 0.7f;

    private float lastNotifyTime = -999f;

    private void OnCollisionEnter(Collision collision)
    {
        if (1 << collision.gameObject.layer == 0)
            return;

        if (Time.time - lastNotifyTime < notifyCooldown)
            return;

        lastNotifyTime = Time.time;

        MonsterLegless monster = FindFirstObjectByType<MonsterLegless>();

        if (monster == null)
            return;

        monster.ThrownObject(transform.position);
    }
}
