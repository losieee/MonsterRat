using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using UnityEngine.SceneManagement;
using System.Linq;

public class SafeZoneTrigger : NetworkBehaviour
{
    public string lobbySceneName = "2Stage";
    public float timeToEvacuate = 3.0f;

    [Header("UI")]
    public GameObject interactionUI;
    public Image progressCircle;
    public TextMeshProUGUI infoText;

    [Header("오염물질 레이어 설정")]
    public LayerMask checkLayers;

    private bool isPlayerInZone = false;
    private float currentTimer = 0f;
    private bool hasSaved = false;

    [Networked] public int PlayersInZoneCount { get; set; }
    [Networked] public NetworkBool IsDoorOpened { get; set; }
    [Networked] public NetworkBool IsEvacuating { get; set; }

    void Start()
    {
        if (interactionUI != null) interactionUI.SetActive(false);
    }

    void Update()
    {
        if (Object == null || !Object.IsValid) return;

        //모든 플레이어가 각자 저장
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
                //게이지를 다 채우면 즉시 실행
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

        //버튼을 누른 당사자는  그 즉시 자기 데이터를 저장
        SaveMyInventoryLocally();
        hasSaved = true;

        // 호스트에게 모든 플레이어를 다음 스테이지로 넘겨달라고 요청
        RPC_RequestEvacuation();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestEvacuation(RpcInfo info = default)
    {
        //호스트한테만 실행됨
        if (IsEvacuating) return;

        int requiredPlayerCount = GetCurrentRoomPlayerCount();
        if (PlayersInZoneCount >= requiredPlayerCount)
        {
            // 방장이 네트워크 변수를 켜서 모든 클라이언트에게 "저장 시작해!"라고 신호를 보냅니다.
            IsEvacuating = true;
            StartEvacuation();
        }
    }
    public void OpenSafeZone()
    {
        if (HasStateAuthority)
        {
            IsDoorOpened = true;
        }
    }
    async void StartEvacuation()
    {
        if (!HasStateAuthority) return;

        // 1스테이지 잔여물 체크 (JSON 활용을 위해 PlayerPrefs 저장)
        bool hasLeftover = CheckForPollution();
        PlayerPrefs.SetInt("IsPollutionLeft", hasLeftover ? 1 : 0);
        PlayerPrefs.Save();

       
       
        //await System.Threading.Tasks.Task.Delay(1500); // 딜레이 굳이 필요한지 모르겠음

        // 씬 전환
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Resources/Scenes/Woong/{lobbySceneName}.unity");
        if (sceneIndex >= 0)
        {
            Runner.LoadScene(SceneRef.FromIndex(sceneIndex));
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

        if (inv != null)
        {
            inv.SaveInventoryData();
            Debug.Log("[SafeZone] 내 캐릭터 인벤토리 로컬 저장 완료");
        }
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