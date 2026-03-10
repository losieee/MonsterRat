using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;  
using UnityEngine.SceneManagement; // 씬 전환용
public class SafeZoneTrigger : NetworkBehaviour
{
    public string lobbySceneName = "GameRoomScene"; // 돌아갈 대기룸 씬 이름
    public float timeToEvacuate = 3.0f; // E키 누르는 시간
    public bool requireAllPlayers = false; // 전원 도착해야 출발 (보류 기능)

    [Header("UI (선택사항)")]
    public GameObject interactionUI;
    public Image progressCircle;
    public TextMeshProUGUI infoText;

    private bool isPlayerInZone = false;
    private float currentTimer = 0f;
    private bool isEvacuating = false;

    void Start()
    {
        if (interactionUI != null) interactionUI.SetActive(false);
    }

    void Update()
    {
        if (isEvacuating) return;
        if (!isPlayerInZone) return;

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
        // 이건 일단 보류
        /* if (requireAllPlayers && Runner.SessionInfo.PlayerCount > currentPlayersInZone) {
            return;
        }
        */
        if (isEvacuating) return;

        // 중복 실행 방지
        isEvacuating = true;

        // Fusion 2의 방장 권한 체크
        if (Runner.IsServer)
        {
            StartEvacuation();
        }
        else
        {
            // 방장이 아니면 방장(StateAuthority)에게 RPC 전송
            RPC_RequestEvacuation();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestEvacuation(RpcInfo info = default)
    {
        StartEvacuation();
    }

    void StartEvacuation()
    {
        if (!Runner.IsServer) return;

        if (Runner.SessionInfo != null)
        {
            Runner.SessionInfo.IsOpen = true;
            Runner.SessionInfo.IsVisible = true;
        }

        Debug.Log("안전 구역 도착! 대기룸으로 대피 중...");

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{lobbySceneName}.unity");
        if (sceneIndex >= 0)
        {
            Runner.LoadScene(SceneRef.FromIndex(sceneIndex));
        }
        else
        {
            Debug.LogError($"'{lobbySceneName}' 씬을 찾을 수 없습니다! Build Settings와 경로를 확인해주세요.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isEvacuating) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.HasInputAuthority)
            {
                isPlayerInZone = true;
                if (interactionUI != null) interactionUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.HasInputAuthority)
            {
                isPlayerInZone = false;
                currentTimer = 0f;
                if (progressCircle != null) progressCircle.fillAmount = 0f;
                if (interactionUI != null) interactionUI.SetActive(false);
            }
        }
    }
}