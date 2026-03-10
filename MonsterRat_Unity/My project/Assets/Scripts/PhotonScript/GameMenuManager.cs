using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion; // Photon.Pun 대신 Fusion 추가

public class GameMenuManager : MonoBehaviour
{
    public GameObject escPanel;
    private bool isPanelActive = false;

    void Update()
    {
        // UI 조작은 로컬 입력이므로 FixedUpdateNetwork가 아닌 기존 Update를 그대로 씁니다.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        isPanelActive = !isPanelActive;
        escPanel.SetActive(isPanelActive);

        if (isPanelActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // async 키워드를 붙여서 비동기 함수로 만들어 줍니다.
    public async void OnClick_LeaveGame()
    {
        // 1. 현재 씬에서 열일하고 있는 NetworkRunner를 찾습니다.
        NetworkRunner runner = FindObjectOfType<NetworkRunner>();

        if (runner != null)
        {
            // 2. PUN2의 LeaveRoom() 역할입니다! 네트워크 연결을 완전히 끊어냅니다.
            // await를 붙여서 끊어질 때까지 안전하게 기다립니다.
            await runner.Shutdown();
        }

        // 3. (PUN2의 OnLeftRoom 역할) 연결이 끊어지면 바로 로비 씬으로 이동합니다.
        SceneManager.LoadScene("LobbyScene"); // 실제 로비 씬 이름과 똑같이 맞춰주세요!

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ==========================================
    // ?? [참고] 호스트가 나가면 방 폭파 (주석 처리하셨던 부분)
    // Fusion 2의 Host 모드에서는 방장(Host)이 Runner.Shutdown()을 해버리면 
    // 서버 자체가 사라지기 때문에 클라이언트들도 자동으로 튕겨 나갑니다!
    // 따라서 RPC로 일일이 킥(Kick)할 필요 없이 위 코드 하나로 완벽하게 해결됩니다.
    // ==========================================
}