using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

public class SafeZoneTrigger : MonoBehaviourPun
{
    public string lobbySceneName = "GameRoomScene"; // 돌아갈 대기룸 씬 2스테이지로 또 바꿔줘야함
    public float timeToEvacuate = 3.0f; // E키 누르는 시간
    public bool requireAllPlayers = false; // 전원 도착해야 출발 True로 바꾸고 구역 도착시 다같이 가능

    [Header("UI (선택사항)")]
    public GameObject interactionUI; 
    public Image progressCircle;
    public TextMeshProUGUI infoText;

    private bool isPlayerInZone = false;
    private float currentTimer = 0f;
    private bool isInteractionCompleted = false;
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
        /* if (requireAllPlayers && PhotonNetwork.CurrentRoom.PlayerCount > currentPlayersInZone) {
            return;
        }
        */
        if (isEvacuating) return;
        if (PhotonNetwork.IsMasterClient)
        {
            StartEvacuation();
        }
        else
        {
            photonView.RPC("RPC_RequestEvacuation", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    void RPC_RequestEvacuation()
    {
        if (PhotonNetwork.IsMasterClient) StartEvacuation();
    }

    void StartEvacuation()
    {

        if (PhotonNetwork.LevelLoadingProgress > 0 && PhotonNetwork.LevelLoadingProgress < 1) return;

        // 대기룸으로 돌아갈 때 방을 다시 열어서 다른 사람이 들어오게 함
        PhotonNetwork.CurrentRoom.IsOpen = true;
        PhotonNetwork.CurrentRoom.IsVisible = true;

        // 동기화 켜기
        PhotonNetwork.AutomaticallySyncScene = true;

        // 대기룸 씬 로드
        PhotonNetwork.LoadLevel(lobbySceneName);
    }

  
    private void OnTriggerEnter(Collider other)
    {

        if (isEvacuating) return;
        if (other.CompareTag("Player") && other.GetComponent<PhotonView>().IsMine)
        {
            isPlayerInZone = true;
            if (interactionUI != null) interactionUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<PhotonView>().IsMine)
        {
            isPlayerInZone = false;
            currentTimer = 0f;
            if (interactionUI != null) interactionUI.SetActive(false);
        }
    }
}