using UnityEngine;
using UnityEngine.SceneManagement; // 씬 이동용
using Photon.Pun;

public class GameMenuManager : MonoBehaviourPunCallbacks
{
    public GameObject escPanel;  
    private bool isPanelActive = false;

    void Update()
    {
        // ESC 키 입력 감지
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

    public void OnClick_LeaveGame()
    {
        PhotonNetwork.LeaveRoom();
        //이건 호스트 나가면 방폭파
        // if (PhotonNetwork.IsMasterClient)
        // {
        //     photonView.RPC("RPC_KickAllPlayers", RpcTarget.All);
        // }
        // else
        // {
        //     PhotonNetwork.LeaveRoom();
        // }
    }

   // [PunRPC]
    //void RPC_KickAllPlayers()
    //{
    //    PhotonNetwork.LeaveRoom();
    //}

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("LobbyScene");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}