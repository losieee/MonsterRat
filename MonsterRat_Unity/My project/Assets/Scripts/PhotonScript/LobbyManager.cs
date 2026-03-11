using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("UI 패널입니다")]
    public GameObject LoginPanel;      // 닉네임 입력 패널
    public GameObject MainPanel;       // 메인 메뉴 패널
    public GameObject RoomListPanel;   // 방 목록 패널
    public GameObject CreateRoomPanel; // 방 만들기 패널

    [Header("입력 UI랑 프리팹 등등")]
    public Button goToRoomListButton;
    public TMP_InputField nicknameInput;
    public TMP_InputField roomnameInput;
    public Transform roomlistContent;
    public GameObject roomItemPrefab;

    private NetworkRunner _runner;

    void Start()
    {
        if (goToRoomListButton != null)
        {
            goToRoomListButton.interactable = false;
        }
    }

    public void OnClickPlay()
    {
        LoginPanel.SetActive(true);
        MainPanel.SetActive(false);
        RoomListPanel.SetActive(false);
        CreateRoomPanel.SetActive(false);
    }

    private void InitializeRunner()
    {
        if (_runner == null)
        {
            _runner = GetComponent<NetworkRunner>();
            _runner.ProvideInput = true;
        }
    }

    #region UI Buttons

    public async void OnClick_SubmitNickname()
    {
        if (string.IsNullOrEmpty(nicknameInput.text)) return;

        string name = nicknameInput.text;
        PlayerPrefs.SetString("PlayerName", name);


        InitializeRunner();

        var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);

        if (result.Ok)
        {
            Debug.Log("버튼 입력 완료 마스터 서버(로비) 입장 성공!");
            if (goToRoomListButton != null) goToRoomListButton.interactable = true;
        }
        else
        {
            Debug.LogError($"로비 접속 실패: {result.ShutdownReason}");
        }

        LoginPanel.SetActive(false);
        MainPanel.SetActive(true);
    }

    public void OnClick_GoToRoomList()
    {
        if (_runner == null || !_runner.IsCloudReady)
        {
            Debug.LogWarning("이 로그가 뜨는 이유는 아직 마스터 클라이언트 서버가 연결이 되지 않았다는 뜻");
            return;
        }

        RoomListPanel.SetActive(true);
        CreateRoomPanel.SetActive(false);
        Debug.Log("로비 접속 시도 완료했습니다. 방 목록이 업데이트 될 겁니다.");
    }

    public void OnClick_OpenCreateRoomPanel()
    {
        CreateRoomPanel.SetActive(true);
        RoomListPanel.SetActive(false);
        roomnameInput.text = "";
    }

    public void OnClick_CancelCreateRoom()
    {
        CreateRoomPanel.SetActive(false);
    }

    public void OnClick_CancelLogin()
    {
        LoginPanel.SetActive(false);
    }

    public async void OnClick_ConfirmCreateRoom()
    {
        if (_runner == null || !_runner.IsCloudReady)
        {
            Debug.LogWarning("서버 접속중 디버그 로그에요 놀라지마십쇼");
            return;
        }

        string roomName = string.IsNullOrEmpty(roomnameInput.text)
            ? $"{PlayerPrefs.GetString("PlayerName", "Player")}'s Room"
            : roomnameInput.text;

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host, 
            SessionName = roomName,
            PlayerCount = 2, // 팀장님이 2인에서 4인으로 바꾼다고 하면 여기 숫자 를 바꿔주세요
            SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>()
        };

        CreateRoomPanel.SetActive(false);

        // 방 생성 시도
        var result = await _runner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log("이 로그가 떴다면 방 입장(생성) 성공했다는 뜻입니다.");
            // 방장만 게임 씬을 로드 
            _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/Woong/GameRoomScene.unity")));
        }
        else
        {
            Debug.LogError($"방 생성 실패: {result.ShutdownReason}");
        }
    }

    public void OnClick_Quit() => Application.Quit();

    #endregion

    #region Fusion Callbacks (PUN2의 콜백들)

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        foreach (Transform child in roomlistContent)
        {
            Destroy(child.gameObject);
        }

        foreach (SessionInfo info in sessionList)
        {
            if (!info.IsVisible || !info.IsOpen) continue;

            GameObject newItem = Instantiate(roomItemPrefab, roomlistContent);
            newItem.GetComponent<RoomItem>().Setup(info, this);
        }
    }

    public async void JoinRoom(string roomName)
    {
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client,  
            SessionName = roomName,
            SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>()
        };

        var result = await _runner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log("이 로그가 떴다면 방 입장 성공했다는 뜻입니다.");
        }
        else
        {
            Debug.LogError($"방 입장 실패: {result.ShutdownReason}");
        }
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("방장이 변경되었습니다! 마이그레이션 처리 필요");
    }

     

    #endregion

    #region Unused Fusion Callbacks (인터페이스를 위해 구현만 해두고 비워둡니다)
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
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
    public void OnHostMigrationCleanUp(NetworkRunner runner) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    #endregion
}