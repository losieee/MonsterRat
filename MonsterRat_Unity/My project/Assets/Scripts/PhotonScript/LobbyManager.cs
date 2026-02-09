using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI 패널입니다")]
    public GameObject LoginPanel;      // 닉네임 입력 패널
    public GameObject MainPanel;       // 메인 메뉴 패널
    public GameObject RoomListPanel;   // 방 목록 패널
    public GameObject CreateRoomPanel; // 방 만들기 패널

    [Header("입력 UI랑 프리팹 등등")]
    //public Button StartButton;
    public Button goToRoomListButton;
    public TMP_InputField nicknameInput;
    public TMP_InputField roomnameInput;
    public Transform roomlistContent;
    public GameObject roomItemPrefab;

    void Start()
    {

        if (goToRoomListButton != null)
        {
            goToRoomListButton.interactable = false;
        }


        LoginPanel.SetActive(true);
        MainPanel.SetActive(false);
        RoomListPanel.SetActive(false);
        CreateRoomPanel.SetActive(false);

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    #region UI Buttons
    public void OnClick_SubmitNickname()
    {
        if (string.IsNullOrEmpty(nicknameInput.text)) return;

        string name = nicknameInput.text;
        PlayerPrefs.SetString("PlayerName", name);
        PhotonNetwork.NickName = name;  

        PhotonNetwork.ConnectUsingSettings();

        LoginPanel.SetActive(false);
        MainPanel.SetActive(true);
    }

    public void OnClick_GoToRoomList()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("이 로그가 뜨는 이유는 아직 마스터 클라이언트 서버가 연결이 되지 않았다는 뜻");
            return;
        }

        MainPanel.SetActive(false);
        RoomListPanel.SetActive(true);

        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public void OnClick_OpenCreateRoomPanel()
    {
        CreateRoomPanel.SetActive(true);
        roomnameInput.text = "";
    }

    public void OnClick_CancelCreateRoom()
    {
        CreateRoomPanel.SetActive(false);
    }

    public void OnClick_ConfirmCreateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("서버 접속중 디버그 로그에요 놀라지마십쇼");
            return;
        }

        string roomName = string.IsNullOrEmpty(roomnameInput.text)
            ? $"{PhotonNetwork.NickName}'s Room"
            : roomnameInput.text;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; // 이거 방 인원수 입니다. 혹시 팀장님이 2인에서 4인으로 바꾼다고 하면 저거 변수 바꿔주세요

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void OnClick_Quit() => Application.Quit();

    #endregion

    #region Photon Callbacks


    public override void OnConnectedToMaster()
    {
        Debug.Log("버튼 입력 완료 마스터 서버 입장시도 ");

        if (goToRoomListButton != null)
        {
            goToRoomListButton.interactable = true;
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("로비 접속 시도 완료했습니다. 아마 1초에서 2초뒤 방 목록 보일겁니다.");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList) // ㅅ새로고침
    {
        foreach (Transform child in roomlistContent)
        {
            Destroy(child.gameObject);
        }

        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList) continue;

            GameObject newItem = Instantiate(roomItemPrefab, roomlistContent);
            newItem.GetComponent<RoomItem>().Setup(info, this);
        }
    }

    // 
    public override void OnJoinedRoom()
    {
        Debug.Log("이 로그가 떴다면 방 입장 성공했다는 뜻입니다.");
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameRoomScene");
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패: {message}");
        CreateRoomPanel.SetActive(false);
    }

    #endregion

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        // 2/3일 추가
        if (newMasterClient.IsLocal)
        {
            Debug.Log("내가 새로운 방장이 되었습니다!");
           
        }
    }
}