using UnityEngine;
public class PlayerUIState : MonoBehaviour
{
    public GameObject storePanel;

    bool inStoreZone;
    bool uiOpen;

    public bool IsUIOpen => uiOpen;

    void Update()
    {
        if (!inStoreZone) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            uiOpen = !uiOpen;

            if (storePanel != null)
                storePanel.SetActive(uiOpen);

            if (uiOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Store"))
            inStoreZone = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Store")) return;

        inStoreZone = false;

        uiOpen = false;

        if (storePanel != null)
            storePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
