using UnityEngine;
using Fusion;

public class ColliderSyncDebugger : NetworkBehaviour
{
    private Rigidbody rb;
    private Collider col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public override void Render()
    {
        if (!HasStateAuthority && rb != null)
        {
            rb.position = transform.position;
            rb.rotation = transform.rotation;

            if (rb.IsSleeping())
            {
                rb.WakeUp();
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (col == null) col = GetComponent<Collider>();

        if (rb != null && col != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.2f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);

            float distance = Vector3.Distance(transform.position, rb.position);
            if (distance > 0.1f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, rb.position);
            }
        }
    }
}