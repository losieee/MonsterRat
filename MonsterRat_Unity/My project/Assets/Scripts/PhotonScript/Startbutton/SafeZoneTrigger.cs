using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using UnityEngine.SceneManagement;
using System.Linq;

public class SafeZoneTrigger : NetworkBehaviour
{
    [Header("스테이지 및 세이브 설정")]
    public string currentStageName = "1Stage"; // 현재 씬 이름
    public string lobbySceneName = "2Stage";   // 넘어갈 씬 이름
    public bool canCreateSaveFile = true;      // true면 이 씬을 클리어했을 때 세이브 파일 생성

    [Header("함선(스폰) 보관 구역 설정")]
    public Transform shipCenter;       // 콜라이더 정중앙 기준점 
    public Collider shipStorageArea;   // 아이템을 스캔할 트리거 콜라이더
    public LayerMask itemLayer;        // 아이템 레이어

    public float timeToEvacuate = 3.0f;

    [Header("UI 및 클리어 조건")]
    public GameObject interactionUI;
    public Image progressCircle;
    public TextMeshProUGUI infoText;
    public LayerMask checkLayers;

    private bool isPlayerInZone = false;
    private float currentTimer = 0f;
    private bool hasSaved = false;

    [Networked] public int PlayersInZoneCount { get; set; }
    [Networked] public NetworkBool IsDoorOpened { get; set; }
    [Networked] public NetworkBool IsEvacuating { get; set; }

    public void OpenSafeZone()
    {
        if (HasStateAuthority)
        {
            IsDoorOpened = true;
        }
    }

    void Start()
    {
        if (interactionUI != null) interactionUI.SetActive(false);
    }

    void Update()
    {
        if (Object == null || !Object.IsValid) return;

        if (IsEvacuating)
        {
            if (!hasSaved)
            {
                hasSaved = true;
                SaveMyInventoryLocally();
            }
            if (infoText != null) infoText.text = "이동 중...";
            return;
        }

        if (!isPlayerInZone || !IsDoorOpened) return;

        int requiredPlayerCount = GetCurrentRoomPlayerCount();
        if (PlayersInZoneCount < requiredPlayerCount)
        {
            if (infoText != null) infoText.text = $"다른 플레이어 대기 중... ({PlayersInZoneCount}/{requiredPlayerCount})";
            return;
        }

        if (infoText != null) infoText.text = "E 키를 길게 눌러 탈출하세요";

        if (Input.GetKey(KeyCode.E))
        {
            currentTimer += Time.deltaTime;
            if (progressCircle != null) progressCircle.fillAmount = currentTimer / timeToEvacuate;

            if (currentTimer >= timeToEvacuate)
            {
                EvacuateToLobby();
            }
        }
        else
        {
            currentTimer = 0f;
            if (progressCircle != null) progressCircle.fillAmount = 0f;
        }
    }

    void EvacuateToLobby()
    {
        if (IsEvacuating) return;
        SaveMyInventoryLocally();
        hasSaved = true;
        RPC_RequestEvacuation();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestEvacuation(RpcInfo info = default)
    {
        if (IsEvacuating) return;

        ClearManager.Instance.ResetProgress();
        int requiredPlayerCount = GetCurrentRoomPlayerCount();
        if (PlayersInZoneCount >= requiredPlayerCount)
        {
            IsEvacuating = true;
            StartEvacuation();
        }
    }

    async void StartEvacuation()
    {
        if (!HasStateAuthority) return;

        bool hasLeftover = CheckForPollution();

        //방장이 월드 상태와 함선 내 아이템을 스캔하여 저장
        SaveWorldState(hasLeftover);

        await System.Threading.Tasks.Task.Delay(1500);

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Resources/Scenes/Woong/{lobbySceneName}.unity");
        if (sceneIndex >= 0)
        {
            Runner.LoadScene(SceneRef.FromIndex(sceneIndex));
        }
    }

    private void SaveWorldState(bool hasLeftover)
    {
        // 1. 공통 작업: 씬 이름, 문 상태, 함선 보관 구역 내 아이템 스캔을 무조건 실행합니다!
        WorldSaveData worldData = new WorldSaveData();
        worldData.savedStageName = lobbySceneName;
        worldData.isDoorActive = hasLeftover;

        // 아이템 스캔 로직 (여기까지 무사히 도달함)
        if (shipStorageArea != null && shipCenter != null)
        {
            Collider[] itemsInShip = Physics.OverlapBox(shipStorageArea.bounds.center, shipStorageArea.bounds.extents, shipStorageArea.transform.rotation, itemLayer);

            foreach (var hit in itemsInShip)
            {
                ItemObject itemObj = hit.GetComponent<ItemObject>();
                if (itemObj != null)
                {
                    ShipItemData sItem = new ShipItemData();
                    sItem.itemID = itemObj.itemData.itemID;

                    // 상대 좌표 계산
                    sItem.localPosition = shipCenter.InverseTransformPoint(itemObj.transform.position);
                    sItem.localRotation = Quaternion.Inverse(shipCenter.rotation) * itemObj.transform.rotation;

                    worldData.shipItems.Add(sItem);
                    Debug.Log($"[WorldSave] 스폰 구역 아이템 스캔됨: {itemObj.itemData.itemName}");
                }
            }
        }

        string json = JsonUtility.ToJson(worldData);

        // 2. 저장 방식 분기: 1스테이지(임시 전달) vs 2스테이지 이상(정식 세이브 슬롯)
        if (currentStageName == "1Stage" && !canCreateSaveFile)
        {
            // 1스테이지는 로비에서 이어하기 버튼이 생기지 않도록, 다음 씬에 넘겨주기 위한 용도로만 저장합니다.
            PlayerPrefs.SetInt("IsPollutionLeft", hasLeftover ? 1 : 0);
            PlayerPrefs.SetString("MasterWorldSave", json); // 2스테이지가 읽을 수 있도록 임시 박스에 넣음
            PlayerPrefs.Save();

            Debug.Log("[WorldSave] 1Stage -> 2Stage 아이템 데이터 전달 완료!");
        }
        else
        {
            // 2스테이지부터는 로비에서 [이어하기]가 가능하도록 선택한 슬롯 번호에 정식으로 저장합니다!
            int activeSlot = PlayerPrefs.GetInt("CurrentActiveSaveSlot", 0);
            string saveKey = "SaveSlot_" + activeSlot;

            PlayerPrefs.SetString(saveKey, json);

            // 씬을 넘어갈 때 WorldLoadManager가 바로 읽을 수 있도록 MasterWorldSave에도 똑같이 덮어씌워 줍니다.
            PlayerPrefs.SetString("MasterWorldSave", json);

            PlayerPrefs.Save();
            Debug.Log($"[WorldSave] {saveKey} 슬롯에 월드 정식 세이브 완료! 다음 스테이지: {worldData.savedStageName}");
        }
    }

    private bool CheckForPollution()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.activeInHierarchy && ((1 << obj.layer) & checkLayers) != 0) return true;
        }
        return false;
    }

    private void SaveMyInventoryLocally()
    {
        PhotonInventory inv = GetComponent<PhotonInventory>();
        if (inv == null) inv = FindObjectsOfType<PhotonInventory>().FirstOrDefault(x => x.HasInputAuthority);

        if (inv != null) inv.SaveInventoryData();
    }

    private int GetCurrentRoomPlayerCount()
    {
        if (Runner == null) return 1;
        int count = 0;
        foreach (var player in Runner.ActivePlayers) count++;
        return Mathf.Max(1, count);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || !Object.IsValid || !HasStateAuthority) return;
        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                PlayersInZoneCount++;
                RPC_SetZoneUI(netObj.InputAuthority, true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (Object == null || !Object.IsValid || !HasStateAuthority) return;
        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                PlayersInZoneCount--;
                RPC_SetZoneUI(netObj.InputAuthority, false);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetZoneUI(PlayerRef targetPlayer, NetworkBool isInside)
    {
        if (Runner.LocalPlayer == targetPlayer)
        {
            isPlayerInZone = isInside;
            if (interactionUI != null) interactionUI.SetActive(isInside);
        }
    }
}