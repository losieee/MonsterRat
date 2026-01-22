using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Fusion.Sockets;
using System;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("UI 패널입니다")]
    public GameObject LoginPanel;      //닉네임이에요
    public GameObject MainPanel;       //버튼 3개 있을 패널 게임시작이랑 옵션 , 나가기 버튼
    public GameObject RoomListPanel;   //이제 게임 시작하기 누르면 리썰컴퍼니처럼 게임목록이 나옵니다
    public GameObject CreateRoomPanel; //방만들기 패널이에여

    [Header("입력 UI랑 프리팹 등등")]
    public TMP_InputField nicknameInput; // 닉네임 입력 UI
    public TMP_InputField roomnameInput; // 방 제목 입력 ㅕㅑ
    public Transform roomlistContent;    // 스크롤 뷰에 있는 content
    public GameObject roomItemPrefab;    // 방 목록에서 참가버튼 아이템프리팹입니다. 이건 프리팹 파일에 포톤파일에 있습니다

    private NetworkRunner _runner;

    void Start()
    {
        // 시작 시 초기화
        LoginPanel.SetActive(true);
        MainPanel.SetActive(false);
        RoomListPanel.SetActive(false);
        CreateRoomPanel.SetActive(false); // 팝업창은 꺼둠
    }

    public void OnClick_SubmitNickname()
    {
        if (string.IsNullOrEmpty(nicknameInput.text)) return;
        PlayerPrefs.SetString("PlayerName", nicknameInput.text);
        // 플레이어 이름 설정하면 그 닉네임을 계속 사용할 수 있게끔 했습니다. 
        // 게임 다시 시작하면 물론 다시 입력해야합니다. 

        LoginPanel.SetActive(false);
        MainPanel.SetActive(true);
    }

    //게임 시작누르면 룸리스트로 이동하는 함수
    public void OnClick_GoToRoomList()
    {
        MainPanel.SetActive(false);
        RoomListPanel.SetActive(true);
        JoinLobby(); // 로비접속 
    }

    public void OnClick_OpenCreateRoomPanel()
    {
        // 방만들기 패널 활성화
        CreateRoomPanel.SetActive(true);

        // 이전에 쓴 방 이름 초기화
        roomnameInput.text = "";
    }

    //이건 방만들다가 취소할때 쓸 함수입니다
    public void OnClick_CancelCreateRoom()
    {
        CreateRoomPanel.SetActive(false);
    }

    // 방 만들기 버튼 누르면 리썰컴퍼니 처럼 게임대기룸으로 이동합니다.
    public async void OnClick_ConfirmCreateRoom()
    {
        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.AddCallbacks(this);

        // 이건 GPT가 알려줬습니다. 이렇게 하면 편하다네요 
        string roomName = string.IsNullOrEmpty(roomnameInput.text)
            ? $"{PlayerPrefs.GetString("PlayerName")}'s Room"
            : roomnameInput.text;

        
        var sceneIndex = SceneUtility.GetBuildIndexByScenePath("GameRoomScene"); // 여기가 리썰컴퍼니 함선 대기실처럼 만들어질 대기룸 씬
        var sceneRef = SceneRef.FromIndex(sceneIndex);

        Debug.Log($"방 생성 시작ㄱ: {roomName}");

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            PlayerCount = 2, // 이거 혹시 나중에 팀장님이 4인용으로 만들 수 도 있어서 방 인원 제한은 여기에서 바꿀 수 있도록 했습니다.
                                // 나중에 원하시면 변수로 만들어서 그냥 편하게 바꿀 수 있도록 할게요
            Scene = sceneRef, // 게임룸으로 이동
            SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>() ?? _runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnClick_Quit() => Application.Quit();

    // ---------------- Fusion 로직 ---------------- 여기도 Fusion 전용 스크립트라 AI도움을 받았습니다.

    async void JoinLobby()
    {
        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.AddCallbacks(this);
        await _runner.JoinSessionLobby(SessionLobby.ClientServer);
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        foreach (Transform child in roomlistContent) Destroy(child.gameObject);
        foreach (SessionInfo session in sessionList)
        {
            if (session.PlayerCount >= session.MaxPlayers || !session.IsVisible) continue;
            GameObject newItem = Instantiate(roomItemPrefab, roomlistContent);
            newItem.GetComponent<RoomItem>().Setup(session, this);
        }
    }

    public async void JoinRoom(string roomName)
    {
        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.AddCallbacks(this);

        var sceneIndex = SceneUtility.GetBuildIndexByScenePath("GameRoomScene");
        var sceneRef = SceneRef.FromIndex(sceneIndex);

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = roomName,
            Scene = sceneRef, // 게임룸으로 이동
            SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>() ?? _runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    // 필수 인터페이스 건들지마세요!! 저도 이거 건들면 고칠 수 있을지 모르겠어요
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}