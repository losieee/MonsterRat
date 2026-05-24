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
    private float scanTimer = 0f;
    private int lastItemCount = -1;

    public float timeToEvacuate = 3.0f;

    [Header("UI 및 클리어 조건")]
    public GameObject interactionUI;
    public Image progressCircle;
    public TextMeshProUGUI infoText;
    public LayerMask checkLayers;

    private bool isPlayerInZone = false;
    private float currentTimer = 0f;
    private bool hasSaved = false;

    [Header("스테이지 클리어 보상 ")]
    public int stageClearReward = 300;


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
        if (HasStateAuthority)
        {
            scanTimer += Time.deltaTime;
            if (scanTimer >= 0.5f)
            {
                scanTimer = 0f;
                CheckRealTimeItems(); // 감시 함수 실행
            }
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
        WorldSaveData worldData = new WorldSaveData();
        worldData.savedStageName = lobbySceneName;
        worldData.isDoorActive = hasLeftover;

        if(WorldLoadManager.instance != null)
        {

        }

        // [핵심 추가] 기존 세이브 슬롯에 있던 방 이름(roomName)을 읽어와서 유지시킵니다.
        int activeSlot = PlayerPrefs.GetInt("CurrentActiveSaveSlot", 0);
        string saveKey = "SaveSlot_" + activeSlot;
        string existingJson = PlayerPrefs.GetString(saveKey, "");
        if (!string.IsNullOrEmpty(existingJson))
        {
            WorldSaveData existingData = JsonUtility.FromJson<WorldSaveData>(existingJson);
            worldData.roomName = existingData.roomName;
        }

        // 아이템 스캔 정밀 계산 로직 (유저님 코드 그대로 유지)
        BoxCollider boxCol = shipStorageArea as BoxCollider;
        if (boxCol != null && shipCenter != null)
        {
            Vector3 exactCenter = boxCol.transform.TransformPoint(boxCol.center);
            Vector3 halfExtents = Vector3.Scale(boxCol.size, boxCol.transform.lossyScale) * 0.5f;

            Collider[] itemsInShip = Physics.OverlapBox(exactCenter, halfExtents, boxCol.transform.rotation, itemLayer);

            foreach (var hit in itemsInShip)
            {
                ItemObject itemObj = hit.GetComponent<ItemObject>();
                if (itemObj != null && itemObj.itemData != null)
                {
                    ShipItemData sItem = new ShipItemData();
                    sItem.itemID = itemObj.itemData.itemID;

                    sItem.localPosition = shipCenter.InverseTransformPoint(itemObj.transform.position);
                    sItem.localRotation = Quaternion.Inverse(shipCenter.rotation) * itemObj.transform.rotation;

                    worldData.shipItems.Add(sItem);
                    Debug.Log($"safezonetirgger 아이템 스캔 성공: {itemObj.itemData.itemName}");
                }
            }
        }

        string json = JsonUtility.ToJson(worldData);

        // [핵심 수정] 1스테이지든 2스테이지든 정식 세이브 슬롯에 덮어씌워야 로비에서 2Stage 이어하기가 가능해집니다.
        PlayerPrefs.SetInt("IsPollutionLeft", hasLeftover ? 1 : 0);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.SetString("MasterWorldSave", json);
        PlayerPrefs.Save();

        Debug.Log($"[WorldSave] {saveKey} 슬롯에 저장 완료! 다음 스테이지: {worldData.savedStageName}");
    }

    private void CheckRealTimeItems()
    {
        BoxCollider boxCol = shipStorageArea as BoxCollider;
        if (boxCol == null || shipCenter == null) return;

        // 세이브할 때와 동일한 크기로 스캔
        Vector3 exactCenter = boxCol.transform.TransformPoint(boxCol.center);
        Vector3 halfExtents = Vector3.Scale(boxCol.size, boxCol.transform.lossyScale) * 0.5f;

        Collider[] itemsInShip = Physics.OverlapBox(exactCenter, halfExtents, boxCol.transform.rotation, itemLayer);

        int currentItemCount = 0;
        foreach (var hit in itemsInShip)
        {
            if (hit.GetComponent<ItemObject>() != null)
            {
                currentItemCount++;
            }
        }

        // 아이템 개수가 이전과 달라졌을 때만 로그띄움
        if (lastItemCount != currentItemCount)
        {
           
            if (lastItemCount != -1)
            {
                if (currentItemCount > lastItemCount)
                    Debug.Log($"Safezone에 아이템 들어옴 (현재: {currentItemCount}개 보존 예정)");
                else
                    Debug.Log($"Safezone에 아이템 없어짐 (현재: {currentItemCount}개 보존 예정)");
            }
            lastItemCount = currentItemCount;
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