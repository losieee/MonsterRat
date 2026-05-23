using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class PlayerUIState : MonoBehaviour
{
    public GameObject storePanel;
    public Text coinText;
    public GameObject clearGauge;
    public GameObject pollutionGauge;

    [Header("상점")]
    public Sprite selectNormal;
    public Image selectIMG;
    public TMP_Text selectName;
    public TMP_Text selectInfo;

    private int currentCoin = 300;

    bool inStoreZone;
    bool uiOpen;
    [HideInInspector] public bool storeOpen;

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
        storeOpen = true;

        GetComponentInParent<PlayerMov>().flash.SetActive(false);

        if (storePanel != null)
            storePanel.SetActive(true);

        if (clearGauge != null)
            clearGauge.SetActive(false);

        if (pollutionGauge != null)
            pollutionGauge.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnStoreOpened?.Invoke();
    }

    void CloseStore()
    {
        uiOpen = false;
        storeOpen = false;

        GetComponentInParent<PlayerMov>().flash.SetActive(true);

        if (storePanel != null)
            storePanel.SetActive(false);

        if(clearGauge != null)
            clearGauge.SetActive(true);

        if (pollutionGauge != null)
            pollutionGauge.SetActive(true);

        selectIMG.sprite = selectNormal;
        selectName.text = " ";
        selectInfo.text = "아이템을 선택하세요.";

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        OnStoreClosed?.Invoke();
    }

    public void UseCoin(int val)
    {
        currentCoin -= val;

        coinText.text = $"₩ {currentCoin.ToString()}";
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
