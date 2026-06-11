using UnityEngine;
using Fusion;
using System;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Analytics;

public class PhotonPlayerUIState : NetworkBehaviour
{
    [Header("UI 설정")]
    public GameObject storePanel;
    public float storeDetectRadius = 2.5f;
    public GameObject aimDot;
    public GameObject clearGauge;
    public GameObject pollutionGauge;
    public TMP_Text watcherWarning;
    public TMP_Text completeBuy;

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

    private Coroutine watcherWarningRoutine;
    private int watcherWarningCount = 0;

    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            if (storePanel != null) storePanel.SetActive(false);
            if (aimDot != null) aimDot.SetActive(false);
            if (clearGauge != null) clearGauge.SetActive(false);
            if (pollutionGauge != null) pollutionGauge.SetActive(false);

            if (watcherWarning != null)
                watcherWarning.gameObject.SetActive(false);

            if (completeBuy != null)
                completeBuy.gameObject.SetActive(false);

            enabled = false;
            return;
        }

        if (watcherWarning != null)
        {
            watcherWarning.gameObject.SetActive(false);
            SetWatcherWarningAlpha(0f);
        }

        if (completeBuy != null)
            completeBuy.gameObject.SetActive(false);

        if (storePanel != null)
            storePanel.SetActive(false);

        ApplyMainUIVisible(true);
    }

    void Update()
    {
        if (!HasInputAuthority) return;

        if (GameInputLock.IsLocked)
        {
            if (uiOpen)
                CloseStore();

            ApplyMainUIVisible(false);
            return;
        }

        CheckForStoreRadar();

        if (uiOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseStore();
            return;
        }

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

        ApplyMainUIVisible(!uiOpen);

        if (currentGoldText != null && WorldLoadManager.instance != null)
        {
            currentGoldText.text = $"₩ {WorldLoadManager.instance.SharedGold}";
        }
    }

    void ApplyMainUIVisible(bool visible)
    {
        if (aimDot != null)
            aimDot.SetActive(visible);

        if (clearGauge != null)
            clearGauge.SetActive(visible);

        if (pollutionGauge != null)
            pollutionGauge.SetActive(visible);
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
        if (completeBuy != null)
            completeBuy.gameObject.SetActive(false);

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

            if (completeBuy != null)
                completeBuy.gameObject.SetActive(true);
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

        ApplyMainUIVisible(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        OnStoreOpened?.Invoke();
    }

    void CloseStore()
    {
        uiOpen = false;
        isGlobalStoreOpen = false;

        if (completeBuy != null)
            completeBuy.gameObject.SetActive(false);

        selectIMG.sprite = selectNormal;
        selectInfo.text = "아이템을 선택하세요.";
        selectName.text = " ";
        selectPrice.text = " ";
        selectedItemIndex = -1;

        if (storePanel != null) storePanel.SetActive(false);

        ApplyMainUIVisible(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        OnStoreClosed?.Invoke();
    }

    public void ShowWatcherWarning()
    {
        watcherWarningCount++;

        if (watcherWarningRoutine != null)
            StopCoroutine(watcherWarningRoutine);

        watcherWarningRoutine = StartCoroutine(FadeWatcherWarning(1f));
    }

    public void HideWatcherWarning()
    {
        watcherWarningCount = Mathf.Max(0, watcherWarningCount - 1);

        if (watcherWarningCount > 0)
            return;

        if (watcherWarningRoutine != null)
            StopCoroutine(watcherWarningRoutine);

        watcherWarningRoutine = StartCoroutine(FadeWatcherWarning(0f));
    }

    IEnumerator FadeWatcherWarning(float targetAlpha)
    {
        if (watcherWarning == null)
            yield break;

        watcherWarning.gameObject.SetActive(true);

        float startAlpha = watcherWarning.color.a;
        float time = 0f;
        float duration = 1.2f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            SetWatcherWarningAlpha(alpha);
            yield return null;
        }

        SetWatcherWarningAlpha(targetAlpha);

        if (targetAlpha <= 0f)
            watcherWarning.gameObject.SetActive(false);
    }

    void SetWatcherWarningAlpha(float alpha)
    {
        Color c = watcherWarning.color;
        c.a = alpha;
        watcherWarning.color = c;
    }
}