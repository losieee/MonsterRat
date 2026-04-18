using UnityEngine;

public class PlayerFootStepType : MonoBehaviour
{
    public FootStepRangeType defaultType = FootStepRangeType.Stone;

    [Header("Ground Check")]
    public Transform rayOrigin;
    public float rayStartHeight = 0.2f;
    public float rayDistance = 1.5f;
    public LayerMask groundMask = ~0;

    public FootStepRangeType CurrentRangeType
    {
        get
        {
            Vector3 origin = rayOrigin != null
                ? rayOrigin.position + Vector3.up * rayStartHeight
                : transform.position + Vector3.up * rayStartHeight;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                FootStepType type = hit.collider.GetComponent<FootStepType>();
                if (type != null)
                    return type.footStepType;
            }

            return defaultType;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = rayOrigin != null
            ? rayOrigin.position + Vector3.up * rayStartHeight
            : transform.position + Vector3.up * rayStartHeight;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + Vector3.down * rayDistance);
    }
}