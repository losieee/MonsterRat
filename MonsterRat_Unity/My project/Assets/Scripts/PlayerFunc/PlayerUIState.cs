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

        if (uiOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseStore();
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (uiOpen) CloseStore();
            else OpenStore();
        }
    }

    void OpenStore()
    {
        uiOpen = true;

        if (storePanel != null)
            storePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnStoreOpened?.Invoke();
    }

    void CloseStore()
    {
        uiOpen = false;

        if (storePanel != null)
            storePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        OnStoreClosed?.Invoke();
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

        if (uiOpen)
            CloseStore();
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
