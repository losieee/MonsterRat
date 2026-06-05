using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;


[System.Serializable]
public class SaveSlotData
{
    public string savedStageName;
    public string roomName;
    public float playTime;
}
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

    [Header("세이브 슬롯 패널")]
    public GameObject SaveSlotPanel;
    public Button[] slotButtons;       // 3개의 슬롯 버튼
    public TMP_Text[] slotTexts;       // 슬롯 버튼 안의 텍스트
    public Button[] deleteButtons;     // 3개의 삭제 버튼

    private int currentSelectedSlot = -1;

    private NetworkRunner _runner;
    private bool hasJoinedLobby = false;
    private bool isSubmittingNickname = false;

    private bool isConnecting = false;

    void Start()
    {
        if (goToRoomListButton != null)
        {
            goToRoomListButton.interactable = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnClickPlay()
    {
        LoginPanel.SetActive(true);
        MainPanel.SetActive(false);
        RoomListPanel.SetActive(false);
        CreateRoomPanel.SetActive(false);
        SaveSlotPanel.SetActive(false);
    }

    private void InitializeRunner()
    {
        if (_runner == null)
        {
            _runner = GetComponent<NetworkRunner>();
            if (_runner == null)
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
            }
            _runner.ProvideInput = true;
        }
    }

    #region UI Buttons

    public async void OnClick_SubmitNickname()
    {
        if (isSubmittingNickname) return;
        if (string.IsNullOrEmpty(nicknameInput.text)) return;

        isSubmittingNickname = true;

        string name = nicknameInput.text;
        PlayerPrefs.SetString("PlayerName", name);

        InitializeRunner();

        if (!hasJoinedLobby)    // 로비에 들어간 적 없을때만 실행 (접속 요청 중복 방지)
        {
            var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);

            if (result.Ok)
            {
                hasJoinedLobby = true;
                Debug.Log("버튼 입력 완료 마스터 서버(로비) 입장 성공!");
                if (goToRoomListButton != null) goToRoomListButton.interactable = true;
            }
            else
            {
                Debug.LogError($"로비 접속 실패: {result.ShutdownReason}");
                isSubmittingNickname = false;
                return;
            }
        }

        LoginPanel.SetActive(false);
        MainPanel.SetActive(true);
        isSubmittingNickname = false;
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
        SaveSlotPanel.SetActive(false);
        Debug.Log("로비 접속 시도 완료했습니다. 방 목록이 업데이트 될 겁니다.");
    }

    public void OnClick_OpenCreateRoomPanel()
    {
        CreateRoomPanel.SetActive(true);
        RoomListPanel.SetActive(false);
        roomnameInput.text = "";
    }

   // public void OnClick_CancelCreateRoom()
   // {
   //     CreateRoomPanel.SetActive(false);
   // }

    public void OnClick_CancelLogin()
    {
        LoginPanel.SetActive(false);
    }

  // public async void OnClick_ConfirmCreateRoom()
  // {
  //     if (_runner == null || !_runner.IsCloudReady)
  //     {
  //         Debug.LogWarning("서버 접속중 디버그 로그에요 놀라지마십쇼");
  //         return;
  //     }
  //
  //     string roomName = string.IsNullOrEmpty(roomnameInput.text)
  //         ? $"{PlayerPrefs.GetString("PlayerName", "Player")}'s Room"
  //         : roomnameInput.text;
  //
  //     var startGameArgs = new StartGameArgs()
  //     {
  //         GameMode = GameMode.Host, 
  //         SessionName = roomName,
  //         PlayerCount = 2, // 팀장님이 2인에서 4인으로 바꾼다고 하면 여기 숫자 를 바꿔주세요
  //         SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>(),
  //         EnableClientSessionCreation = true
  //     };
  //     if (HostMigrationManager.Instance != null)
  //         HostMigrationManager.Instance.RegisterRunner(_runner);
  //
  //     CreateRoomPanel.SetActive(false);
  //
  //     // 방 생성 시도
  //     var result = await _runner.StartGame(startGameArgs);
  //
  //     if (result.Ok)
  //     {
  //         Debug.Log("이 로그가 떴다면 방 입장(생성) 성공했다는 뜻입니다.");
  //         Cursor.lockState = CursorLockMode.Locked;
  //         Cursor.visible = false;
  //         // 방장만 게임 씬을 로드 
  //         _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath("Assets/Resources/Scenes/Woong/1Stage.unity")));
  //     }
  //     else
  //     {
  //         Debug.LogError($"방 생성 실패: {result.ShutdownReason}");
  //     }
  // }

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
            SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>(),
            EnableClientSessionCreation = true
        };

        if (HostMigrationManager.Instance != null)
            HostMigrationManager.Instance.RegisterRunner(_runner);

        PlayerPrefs.SetInt("JoinedFromLobby", 1);
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
       // Debug.Log("방장이 변경되었습니다! 마이그레이션 처리 필요");
    }
    #region ★ 세이브 슬롯 로직 (리썰 컴퍼니 방식) ★

    
    public void OnClick_OpenSaveSlotPanel()
    {
        SaveSlotPanel.SetActive(true);
        RoomListPanel.SetActive(false);
        CreateRoomPanel.SetActive(false);
        RefreshSaveSlots(); // 화면 열 때 슬롯 정보 업데이트
    }

    // 세이브 슬롯 3개 열기
    private void RefreshSaveSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            string saveKey = "SaveSlot_" + i;
            if (PlayerPrefs.HasKey(saveKey))
            {
                // 세이브 파일이 있는가?
                string json = PlayerPrefs.GetString(saveKey);
                SaveSlotData data = JsonUtility.FromJson<SaveSlotData>(json);

                slotTexts[i].text = $"[Slot {i + 1}] {data.roomName}\n<size=80%>{data.savedStageName}</size>";  
                deleteButtons[i].gameObject.SetActive(true);
            }
            else
            {
                // 세이브 파일이 없는가?
                slotTexts[i].text = $"[Slot {i + 1}]\nEmpty";
                deleteButtons[i].gameObject.SetActive(false);
            }
        }
    }

    //버튼에 각 slot int 부여해서 0 1 2 눌렀을 시 LobbyManager을 이용해서 그 번호에 맞는 슬롯정보 불러옴
    public void OnClick_SelectSlot(int slotIndex)
    {
        currentSelectedSlot = slotIndex;
        PlayerPrefs.SetInt("CurrentActiveSaveSlot", currentSelectedSlot);
        PlayerPrefs.Save();
        string saveKey = "SaveSlot_" + slotIndex;

        if (PlayerPrefs.HasKey(saveKey))
        {
            StartHostGameFromSave(saveKey);
        }
        else
        {
            // 빈 슬롯을 누름 -> 방 이름 입력 패널 열기
            SaveSlotPanel.SetActive(false);
            CreateRoomPanel.SetActive(true);
            roomnameInput.text = "";
        }
    }

    //슬롯 삭제 버튼
    public void OnClick_DeleteSlot(int slotIndex)
    {
        PlayerPrefs.DeleteKey("SaveSlot_" + slotIndex);
        PlayerPrefs.Save();
        RefreshSaveSlots(); // 지우고 UI 새로고침
    }

    public void OnClick_CancelCreateRoom()
    {
        CreateRoomPanel.SetActive(false);
        SaveSlotPanel.SetActive(true); // 굳이? 뒤로가기 버튼? 
    }
    #endregion

    #region 방 생성 및 게임 시작 (호스트)

    // 빈 슬롯 클릭 후 이름 짓고 [방 생성] 누를 때 
    public async void OnClick_ConfirmCreateRoom()
    {
        if (_runner == null || !_runner.IsCloudReady) return;

        if (PlayTimeManager.Instance != null)
        {
            PlayTimeManager.Instance.ResetPlayTime();
            PlayTimeManager.Instance.StartCounting();
        }

        string roomName = string.IsNullOrEmpty(roomnameInput.text) ? $"{PlayerPrefs.GetString("PlayerName", "Player")}'s Room" : roomnameInput.text;

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            PlayerCount = 2,
            SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>()
        };

        CreateRoomPanel.SetActive(false);
        PlayerPrefs.SetInt("JoinedFromLobby", 1);
        var result = await _runner.StartGame(startGameArgs);

        if (result.Ok)
        {
            //빈 슬롯이면 새스테이지로 그냥 만듦
            SaveSlotData initData = new SaveSlotData { roomName = roomName, savedStageName = "1Stage", playTime = 0f };
            PlayerPrefs.SetString("SaveSlot_" + currentSelectedSlot, JsonUtility.ToJson(initData));
            PlayerPrefs.DeleteKey("MasterWorldSave");
            PlayerPrefs.Save();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 새 게임이므로 무조건 1Stage 로드
            _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath("Assets/Resources/Scenes/Woong/1Stage.unity")));
        }
    }

    // 차있는 슬롯 클릭 시 즉시 방 생성 (이어서 하기)
    private async void StartHostGameFromSave(string saveKey)
    {
        if (_runner == null || !_runner.IsCloudReady) return;

        string json = PlayerPrefs.GetString(saveKey);
        SaveSlotData data = JsonUtility.FromJson<SaveSlotData>(json);

        if (PlayTimeManager.Instance != null)
        {
            PlayTimeManager.Instance.SetPlayTime(data.playTime);
            PlayTimeManager.Instance.StartCounting();
        }

        PlayerPrefs.SetString("MasterWorldSave", json);
        PlayerPrefs.Save();
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = data.roomName, // 저장됐던 방 이름 그대로 사용
            PlayerCount = 2,
            SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>()
        };

        SaveSlotPanel.SetActive(false);
        PlayerPrefs.SetInt("JoinedFromLobby", 1);
        PlayerPrefs.SetInt("SpawnInventoryOnGround", 1);
        var result = await _runner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 저장되어 있던 스테이지 씬을 로드합니다.
            string targetScene = data.savedStageName;
            _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath($"Assets/Resources/Scenes/Woong/{targetScene}.unity")));
        }
    }

    public async void LeaveCurrentGameAndReset()
    {
        if(_runner != null && _runner.IsRunning)
        {
            await _runner.Shutdown();
        }
        hasJoinedLobby = false;
        Cursor.lockState= CursorLockMode.None;
        Cursor.visible = true;
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        if(!string.IsNullOrEmpty(savedName))
        {
            nicknameInput.text = savedName;
            OnClick_SubmitNickname();
        }
        else
        {
            LoginPanel.SetActive(false);
            MainPanel.SetActive(false);
        }
    }
    #endregion


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