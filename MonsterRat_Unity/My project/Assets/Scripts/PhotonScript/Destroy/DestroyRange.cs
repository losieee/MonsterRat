using Fusion;
using UnityEngine;

public class DestroyRange : MonoBehaviour
{
    [Tooltip("소각할 아이템 관련")]
    public LayerMask trashLayer;

    private DestroyController controller;

    private void Awake()
    {
        controller = GetComponentInParent<DestroyController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & trashLayer) != 0)
        {
            NetworkObject netObj = other.transform.root.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                controller.deleteBoxes.Add(netObj);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & trashLayer) != 0)
        {
            NetworkObject netObj = other.transform.root.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                controller.deleteBoxes.Remove(netObj);
            }
        }
    }
}