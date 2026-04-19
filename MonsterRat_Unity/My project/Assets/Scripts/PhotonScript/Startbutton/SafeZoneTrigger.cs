using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using UnityEngine.SceneManagement;

public class SafeZoneTrigger : NetworkBehaviour
{
    public string lobbySceneName = "2Stage"; // 이거 인스펙터에서 바꾸면 됨 ㅇㅇ
    public float timeToEvacuate = 3.0f;

    [Header("UI")]
    public GameObject interactionUI;
    public Image progressCircle;
    public TextMeshProUGUI infoText;

    private bool isPlayerInZone = false;
    private float currentTimer = 0f;

    [Networked] public int PlayersInZoneCount { get; set; }
    [Networked] public NetworkBool IsDoorOpened { get; set; }
    [Networked] public NetworkBool IsEvacuating { get; set; }

    private const int RequiredPlayerCount = 2;

    void Start()
    {
        if (interactionUI != null) interactionUI.SetActive(false);
    }

    public void OpenSafeZone()
    {
        if (HasStateAuthority)
        {
            IsDoorOpened = true;
        }
    }

    void Update()
    {
        if (Object == null || !Object.IsValid) return;

        if (IsEvacuating)
        {
            if (infoText != null) infoText.text = "이게 보인다면 렉걸린거거나 오류임 대기중이라는 뜻이기도함";
            return;
        }

        if (!isPlayerInZone) return;

        if (!IsDoorOpened)
        {
            if (infoText != null) infoText.text = "오염물질을 모두 정화하세요";
            currentTimer = 0f;
            if (progressCircle != null) progressCircle.fillAmount = 0f;
            return;
        }

        if (PlayersInZoneCount < RequiredPlayerCount)
        {
            if (infoText != null) infoText.text = $"다른 플레이어 대기 중... ({PlayersInZoneCount}/{RequiredPlayerCount})";
            currentTimer = 0f;
            if (progressCircle != null) progressCircle.fillAmount = 0f;
            return;
        }
        else
        {
            if (infoText != null) infoText.text = "E 키를 길게 눌러 탈출하세요";
        }

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
        RPC_RequestEvacuation();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestEvacuation(RpcInfo info = default)
    {
        if (IsEvacuating) return;

        if (PlayersInZoneCount >= RequiredPlayerCount)
        {
            IsEvacuating = true;
            StartEvacuation();
        }
    }

    void StartEvacuation()
    {
        if (!HasStateAuthority) return;

        if (Runner.SessionInfo != null)
        {
            Runner.SessionInfo.IsOpen = true;
            Runner.SessionInfo.IsVisible = true;
        }

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Resources/Scenes/Woong/{lobbySceneName}.unity");

        if (sceneIndex >= 0)
        {
            Runner.LoadScene(SceneRef.FromIndex(sceneIndex));
        }
        else
        {
            Debug.LogError($"'{lobbySceneName}' 빌드 경로 제대로 된거 맞냐?Assets/Resources/Scenes/Woong/{lobbySceneName}.unity 이건지 확인하셈");
        }
    }

    //여기 
    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || !Object.IsValid) return;

        // 서버(호스트)가 아니면 여기서 무조건 컷! (중복 실행 방지)
        if (!HasStateAuthority) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                PlayersInZoneCount++;

                // 클라한테 UI 켜라고 전달 
                RPC_SetZoneUI(netObj.InputAuthority, true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (Object == null || !Object.IsValid) return;

        if (!HasStateAuthority) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                PlayersInZoneCount--;
                if (PlayersInZoneCount < 0) PlayersInZoneCount = 0;

                // 클라한테 UI 끄라고 전달
                RPC_SetZoneUI(netObj.InputAuthority, false);
            }
        }
    }

    // 각자 자기 화면의 UI를 켜고 끄는 RPC
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetZoneUI(PlayerRef targetPlayer, NetworkBool isInside)
    {
        // 나에게 온 명령이 맞는지 확인 (내 캐릭터가 들어갔을 때만)
        if (Runner.LocalPlayer == targetPlayer)
        {
            isPlayerInZone = isInside;

            if (interactionUI != null)
            {
                interactionUI.SetActive(isInside);
            }

            if (isInside)
            {
                Debug.Log("탈출 UI On");
            }
            else
            {
                // 나갔을 땐 게이지 완전 초기화
                currentTimer = 0f;
                if (progressCircle != null) progressCircle.fillAmount = 0f;
                Debug.Log("탈출 UI OFF baby");
            }
        }
    }
}