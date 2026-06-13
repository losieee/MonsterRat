using UnityEngine;
using Fusion;
using System;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PhotonPlayerUIState : NetworkBehaviour
{
    [Header("UI 설정")]
    public GameObject storePanel;
    public GameObject aimDot;
    public GameObject clearGauge;
    public GameObject pollutionGauge;
    public TMP_Text watcherWarning;
    public TMP_Text completeBuy;

    [Header("상점 Raycast")]
    public Transform storeRayOrigin;
    public float storeRayDistance = 3f;
    public LayerMask storeRayMask = ~0;

    [Header("상점 Select")]
    public Sprite selectNormal;
    public Image selectIMG;
    public TMP_Text selectName;
    public TMP_Text selectPrice ;
    public TMP_Text selectInfo;
    public TMP_Text currentGoldText;

    bool uiOpen;
    bool storeInputLocked = false;
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

        if (GameInputLock.IsLocked && !storeInputLocked)
        {
            if (uiOpen)
                CloseStore();

            ApplyMainUIVisible(false);
            return;
        }

        if (uiOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseStore();
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (uiOpen)
            {
                CloseStore();
            }
            else
            {
                PhotonToolSpawner lookStore;

                if (TryGetLookStore(out lookStore))
                {
                    currentStore = lookStore;
                    OpenStore();
                }
            }
        }

        ApplyMainUIVisible(!uiOpen);

        if (currentGoldText != null && WorldLoadManager.instance != null)
        {
            currentGoldText.text = $"₩ {WorldLoadManager.instance.SharedGold}";
        }
    }

    public void ApplyMainUIVisible(bool visible)
    {
        if (aimDot != null)
            aimDot.SetActive(visible);

        if (clearGauge != null)
            clearGauge.SetActive(visible);

        if (pollutionGauge != null)
            pollutionGauge.SetActive(visible);
    }

    bool TryGetLookStore(out PhotonToolSpawner store)
    {
        store = null;

        Transform origin = storeRayOrigin;

        if (origin == null && Camera.main != null)
            origin = Camera.main.transform;

        if (origin == null)
            return false;

        Ray ray = new Ray(origin.position, origin.forward);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            storeRayDistance,
            storeRayMask,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
            return false;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            // 내 몸 콜라이더를 맞은 경우 무시
            if (hit.collider.transform.IsChildOf(transform))
                continue;

            PhotonToolSpawner spawner = hit.collider.GetComponentInParent<PhotonToolSpawner>();

            if (spawner == null)
                continue;

            store = spawner;
            return true;
        }

        return false;
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

        storeInputLocked = true;
        GameInputLock.Lock();
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
        currentStore = null;

        if (storePanel != null) storePanel.SetActive(false);

        ApplyMainUIVisible(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (storeInputLocked)
        {
            storeInputLocked = false;
            GameInputLock.Unlock();
        }

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