using UnityEngine;
using Fusion;
using System;

public class PhotonPlayerUIState : NetworkBehaviour
{
    [Header("UI ¼³Á¤")]
    public GameObject storePanel;
    public float storeDetectRadius = 2.5f;
    public GameObject aimDot;
    public GameObject clearGauge;
    public GameObject pollutionGauge;

    bool inStoreZone;
    bool uiOpen;
    public bool IsUIOpen => uiOpen;

    public static bool isGlobalStoreOpen = false;

    public event Action OnStoreOpened;
    public event Action OnStoreClosed;

    private PhotonToolSpawner currentStore;

    void Update()
    {
        if (!HasInputAuthority) return;

        CheckForStoreRadar();

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

    void CheckForStoreRadar()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, storeDetectRadius);
        bool isNearStore = false;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Store"))
            {
                isNearStore = true;
                if (currentStore == null)
                {
                    currentStore = hit.GetComponent<PhotonToolSpawner>();
                }
                break;
            }
        }

        if (isNearStore && !inStoreZone)
        {
            inStoreZone = true;
        }
        else if (!isNearStore && inStoreZone)
        {
            inStoreZone = false;
            currentStore = null;
            if (uiOpen) CloseStore();
        }
    }

    public void OnClickBuyItem(int itemIndex)
    {
        if (currentStore != null)
        {
            NetworkObject storeNetObj = currentStore.GetComponent<NetworkObject>();
            if (storeNetObj != null)
            {
                RPC_RequestSpawnToHost(storeNetObj, itemIndex);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestSpawnToHost(NetworkObject storeObj, int itemIndex)
    {
        if (storeObj != null)
        {
            PhotonToolSpawner spawner = storeObj.GetComponent<PhotonToolSpawner>();
            if (spawner != null && spawner.itemSpawnPoint != null)
            {
                Runner.Spawn(spawner.tools[itemIndex], spawner.itemSpawnPoint.position, spawner.itemSpawnPoint.rotation, null);
            }
        }
    }

    void OpenStore()
    {
        uiOpen = true;
        isGlobalStoreOpen = true;  

        if (storePanel != null) storePanel.SetActive(true);
        if (aimDot != null) aimDot.SetActive(false);
        if (clearGauge != null) clearGauge.SetActive(false);
        if (pollutionGauge != null) pollutionGauge.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        OnStoreOpened?.Invoke();
    }

    void CloseStore()
    {
        uiOpen = false;
        isGlobalStoreOpen = false;  

        if (storePanel != null) storePanel.SetActive(false);
        if (aimDot != null) aimDot.SetActive(true);
        if (clearGauge != null) clearGauge.SetActive(true);
        if (pollutionGauge != null) pollutionGauge.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        OnStoreClosed?.Invoke();
    }
}