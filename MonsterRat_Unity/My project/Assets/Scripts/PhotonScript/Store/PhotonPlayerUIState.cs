using UnityEngine;
using Fusion;
using System;
using TMPro;
using UnityEngine.UI;

public class PhotonPlayerUIState : NetworkBehaviour
{
    [Header("UI 설정")]
    public GameObject storePanel;
    public float storeDetectRadius = 2.5f;
    public GameObject aimDot;
    public GameObject clearGauge;
    public GameObject pollutionGauge;

    [Header("상점 Select")]
    public Sprite selectNormal;
    public Image selectIMG;
    public TMP_Text selectName;
    public TMP_Text selectPrice ;
    public TMP_Text selectInfo;
    public TMP_Text currentGoldText;

    bool inStoreZone;
    bool uiOpen;
    public bool IsUIOpen => uiOpen;

    public static bool isGlobalStoreOpen = false;

    public event Action OnStoreOpened;
    public event Action OnStoreClosed;

    private PhotonToolSpawner currentStore;

    private int selectedItemIndex = -1;

    void Update()
    {
        if (!HasInputAuthority) return;

        if(GameInputLock.IsLocked)
        {
            aimDot.SetActive(false);
            clearGauge.SetActive(false);
            pollutionGauge.SetActive(false);
            storePanel.SetActive(false);
        }

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

        if (currentGoldText != null && WorldLoadManager.instance != null)
        {
            currentGoldText.text = $"₩ {WorldLoadManager.instance.SharedGold}";
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
            selectedItemIndex = -1;
            if (uiOpen) CloseStore();
        }
    }

    public void OnClickBuyItem(int itemIndex)
    {
        selectedItemIndex = itemIndex;

        ItemData itemData = ItemDatabase.Instance.GetItemByIndex(itemIndex);

        if (itemData == null)
            return;

        if (selectIMG != null)
        {
            selectIMG.sprite = itemData.itemIcon;
            selectIMG.enabled = itemData.itemIcon != null;
        }

        if (selectName != null)
            selectName.text = itemData.storeName;

        if (selectPrice != null)
            selectPrice.text = $"₩ {itemData.itemPrice}";

        if (selectInfo != null)
            selectInfo.text = itemData.itenInfo;
    }

    public void OnClickSpawnSelectedItem()
    {
        if (selectedItemIndex < 0 || currentStore == null)
            return;

        ItemData itemData = ItemDatabase.Instance.GetItemByIndex(selectedItemIndex);
        if (currentStore == null)
            return;
        if(WorldLoadManager.instance.SharedGold >= itemData.itemPrice)
        {
            //방장한테 돈 차감하라고 하기 (클라이언트 기준)
            WorldLoadManager.instance.RPC_DeductGold(itemData.itemPrice);
            NetworkObject StoreNetObj = currentStore.GetComponent<NetworkObject>();
            RPC_RequestSpawnToHost(StoreNetObj, selectedItemIndex);
        }
        if (WorldLoadManager.instance == null)
        {
            Debug.LogError("[상점 오류] 현재 씬에 WorldLoadManager가 없습니다! 골드 결제를 진행할 수 없습니다.");
            return;
        }
        NetworkObject storeNetObj = currentStore.GetComponent<NetworkObject>();

        if (storeNetObj == null)
            return;

      //  RPC_RequestSpawnToHost(storeNetObj, selectedItemIndex); 이거 주석풀면 2개 스폰됩니다
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestSpawnToHost(NetworkObject storeObj, int itemIndex)
    {
        if (storeObj != null)
        {
            PhotonToolSpawner spawner = storeObj.GetComponent<PhotonToolSpawner>();
            if (spawner != null && spawner.itemSpawnPoint != null)
            {
                ItemData itemData = ItemDatabase.Instance.GetItemByIndex(itemIndex);
                if (itemData != null && itemData.itemPrefab != null)
                {
                    Runner.Spawn(itemData.itemPrefab, spawner.itemSpawnPoint.position, spawner.itemSpawnPoint.rotation, null);
                }
            }
        }
    }

    void OpenStore()
    {
        uiOpen = true;
        isGlobalStoreOpen = true;  

        if (storePanel != null) storePanel.SetActive(true);
        aimDot.SetActive(false);
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

        selectIMG.sprite = selectNormal;
        selectInfo.text = "아이템을 선택하세요.";
        selectName.text = " ";
        selectPrice.text = " ";
        selectedItemIndex = -1;

        if (storePanel != null) storePanel.SetActive(false);
        aimDot.SetActive(true);
        if (clearGauge != null) clearGauge.SetActive(true);
        if (pollutionGauge != null) pollutionGauge.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        OnStoreClosed?.Invoke();
    }
}