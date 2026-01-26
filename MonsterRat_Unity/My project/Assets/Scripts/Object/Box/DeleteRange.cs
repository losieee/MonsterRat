using UnityEngine;

public class DeleteRange : MonoBehaviour
{
    DeleteObject deleteObject;

    private void Awake()
    {
        deleteObject = GetComponentInParent<DeleteObject>();
    }

    private void OnTriggerEnter(Collider other)
    {
        deleteObject.deleteBoxes.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        deleteObject.deleteBoxes.Remove(other.gameObject);
    }
}
