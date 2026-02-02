using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class GameStartLever : MonoBehaviourPun
{
    [Header("UI 설정")]
    public GameObject uiCanvas;        
    public Image fillImage;            
    public TextMeshProUGUI infoText;   

    [Header("잡다한거")]
    public float holdDuration = 3.0f;  
    public string nextSceneName = "GameScene";  

    private bool isPlayerInZone = false;
    private float currentHoldTime = 0f;
    private bool isInteractionCompleted = false;

    void Start()
    {
        if (uiCanvas != null) uiCanvas.SetActive(false);
        if (fillImage != null) fillImage.fillAmount = 0f;
    }

    void Update()
    {
        if (isInteractionCompleted || !isPlayerInZone) return;

        // E키를 누르고 있는 동안
        if (Input.GetKey(KeyCode.E))
        {
            currentHoldTime += Time.deltaTime;

            if (fillImage != null)
            {
                fillImage.fillAmount = currentHoldTime / holdDuration;
            }

            // 게이지가 다 찼을 때
            if (currentHoldTime >= holdDuration)
            {
                CompleteInteraction();
            }
        }
        else
        {
            // 키를 떼면 초기화  
            currentHoldTime = 0f;
            if (fillImage != null) fillImage.fillAmount = 0f;
        }
    }

    // 플레이어가 트리거 안에 들어왔을 때
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<PhotonView>().IsMine)
        {
            isPlayerInZone = true;
            if (uiCanvas != null) uiCanvas.SetActive(true);
            if (infoText != null) infoText.text = "E키를 꾹 눌러 게임 시작";
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<PhotonView>().IsMine)
        {
            isPlayerInZone = false;
            currentHoldTime = 0f;
            if (fillImage != null) fillImage.fillAmount = 0f;
            if (uiCanvas != null) uiCanvas.SetActive(false);
        }
    }

    void CompleteInteraction()
    {
        isInteractionCompleted = true; // 중복 실행 방지

        if (uiCanvas != null) uiCanvas.SetActive(false); // UI 끄기

        Debug.Log("게임씬으로 넘어감");

        if (PhotonNetwork.IsMasterClient)
        {
            StartGameProcess();
        }
        else
        {
            photonView.RPC("RPC_RequestStartGame", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    void RPC_RequestStartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartGameProcess();
        }
    }

    void StartGameProcess()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // 모든 클라이언트가 씬을 동기화하도록 설정
        PhotonNetwork.AutomaticallySyncScene = true;

        Debug.Log("씬 이동 중...");
        PhotonNetwork.LoadLevel(nextSceneName);
    }
}