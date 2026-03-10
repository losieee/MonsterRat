using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;  
using UnityEngine.SceneManagement;

public class GameStartLever : NetworkBehaviour
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
            currentHoldTime = 0f;
            if (fillImage != null) fillImage.fillAmount = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponent<NetworkObject>();

            if (netObj != null && netObj.HasInputAuthority)
            {
                isPlayerInZone = true;
                if (uiCanvas != null) uiCanvas.SetActive(true);
                if (infoText != null) infoText.text = "E키를 꾹 눌러 게임 시작";
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
                currentHoldTime = 0f;
                if (fillImage != null) fillImage.fillAmount = 0f;
                if (uiCanvas != null) uiCanvas.SetActive(false);
            }
        }
    }

    void CompleteInteraction()
    {
        isInteractionCompleted = true; // 중복 실행 방지

        if (uiCanvas != null) uiCanvas.SetActive(false); // UI 끄기

        Debug.Log("게임씬으로 넘어감");

        if (Object.HasStateAuthority)
        {
            StartGameProcess();
        }
        else
        {
            RPC_RequestStartGame();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestStartGame(RpcInfo info = default)
    {
        StartGameProcess();
    }

    void StartGameProcess()
    {
        if (!Runner.IsServer) return;
        if (Runner.SessionInfo != null)
        {
            Runner.SessionInfo.IsOpen = false;
            Runner.SessionInfo.IsVisible = false;
        }

        Debug.Log("씬 이동 중...");

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/Woong/{nextSceneName}.unity");
        if (sceneIndex >= 0)
        {
            Runner.LoadScene(SceneRef.FromIndex(sceneIndex));
        }
        else
        {
            Debug.LogError($"'{nextSceneName}' 씬을 찾을 수 없습니다! Build Settings와 경로를 확인해주세요.");
        }
    }
}