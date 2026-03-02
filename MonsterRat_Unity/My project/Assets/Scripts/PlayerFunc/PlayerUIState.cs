using System;
using UnityEngine;
public class PlayerUIState : MonoBehaviour
{
    public GameObject storePanel;

    bool inStoreZone;
    bool uiOpen;

    public bool IsUIOpen => uiOpen;

    public event Action OnStoreOpened;
    public event Action OnStoreClosed;

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

                OnStoreOpened?.Invoke();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                OnStoreClosed?.Invoke();
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

        OnStoreClosed?.Invoke();
    }
}
