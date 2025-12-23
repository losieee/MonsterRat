// LobbyManager.cs (NO TMP)
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NativeWebSocket;
using System.Net;
using System.Net.Sockets;

public class LobbyManager : MonoBehaviour
{
    [Header("Server HTTP Base")]
    public string serverHttpBase = "http://127.0.0.1:3000"; // 다른 PC에서는 호스트PC IP로 바꾸기

    [Header("UI")]
    public GameObject lobbyPanel;
    public GameObject roomPanel;
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button startGameButton;
    public Button leaveRoomButton;
    public Transform roomListContent;
    public GameObject roomItemPrefab;

    public InputField roomIdInput;
    public Text statusText;

    private WebSocket ws;

    private string clientId;
    private string currentRoomId = "";

    private bool isHost = false;
    private bool canStart = false;
    private bool inRoom = false;

    private bool prevCanStart = false;
    private int prevPlayerCount = -1;

    private bool refreshingRooms = false;

    // -------- REST DTO --------
    [Serializable] private class CreateRoomRequest { public string clientId; }
    [Serializable] private class CreateRoomResponse { public bool ok; public string roomId; public string hostKey; public string wsUrl; public string error; }

    [Serializable] private class JoinRoomRequest { public string roomId; public string clientId; }
    [Serializable] private class JoinRoomResponse { public bool ok; public string roomId; public string wsUrl; public string error; }

    [Serializable] private class RoomsResponse { public RoomInfo[] rooms; }
    [Serializable] private class RoomInfo { public string roomId; public long createdAt; public int playerCount; public bool started; }

    [Serializable] private class GameStartMsg {
        public string type;
        public string roomId;
        public string sceneName;
        public string hostIp;
        public int port;
    }

    void Start()
    {
        clientId = GetClientId();
        Log($"ClientId: {clientId}");

        // 초기 UI 상태
        startGameButton.gameObject.SetActive(false);
        startGameButton.interactable = false;
        leaveRoomButton.gameObject.SetActive(false);

        createRoomButton.onClick.AddListener(OnCreateRoom);
        joinRoomButton.onClick.AddListener(OnJoinRoom);
        startGameButton.onClick.AddListener(OnStartGame);
        leaveRoomButton.onClick.AddListener(OnLeaveRoom);

        // 방 목록 자동 갱신 (네 코드에 이미 있었던 부분) :contentReference[oaicite:3]{index=3}
        InvokeRepeating(nameof(RefreshRooms), 0.2f, 2f);
        SetScreenLobby();
    }

    private string GetLocalIPv4()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4
                    return ip.ToString();
            }
        }
        catch { }
        return "";
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        ws?.DispatchMessageQueue();
#endif
        // 버튼 표시/활성 상태
        startGameButton.gameObject.SetActive(inRoom && isHost);
        startGameButton.interactable = (inRoom && isHost && canStart);

        leaveRoomButton.gameObject.SetActive(inRoom);
    }

    void OnDestroy()
    {
        _ = CloseWs();
    }

    private string GetClientId()
    {
        const string key = "CLIENT_ID";
        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetString(key, Guid.NewGuid().ToString("N"));
            PlayerPrefs.Save();
        }
        return PlayerPrefs.GetString(key);
    }

    // ---------------------------
    // Room List (중요: 너가 찾던 roomlist)
    // ---------------------------
    private void RefreshRooms()
    {
        if (refreshingRooms) return;
        StartCoroutine(RefreshRoomsCoroutine());
    }

    private IEnumerator RefreshRoomsCoroutine()
    {
        refreshingRooms = true;

        var url = $"{serverHttpBase}/rooms";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            refreshingRooms = false;
            yield break;
        }

        var resp = JsonUtility.FromJson<RoomsResponse>(req.downloadHandler.text);
        BuildRoomButtons(resp);
        refreshingRooms = false;
    }

    // ---------------------------
    // Create / Join
    // ---------------------------
    private void OnCreateRoom() => StartCoroutine(CreateRoomCoroutine());
    private IEnumerator CreateRoomCoroutine()
    {
        Log("방 생성 요청 중...");

        var url = $"{serverHttpBase}/rooms/create";
        var reqObj = new CreateRoomRequest { clientId = clientId };
        var json = JsonUtility.ToJson(reqObj);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Log($"방 생성 실패: {req.error}");
            yield break;
        }

        var resp = JsonUtility.FromJson<CreateRoomResponse>(req.downloadHandler.text);
        if (resp == null || !resp.ok)
        {
            Log($"방 생성 실패: {(resp != null ? resp.error : "UNKNOWN")}");
            yield break;
        }

        currentRoomId = resp.roomId;
        inRoom = true;
        SetScreenRoom();

        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;

        Log($"방 생성 성공! roomId={resp.roomId}");
        RefreshRooms();

        yield return ConnectLobbyWs(resp.wsUrl);
    }

    private void OnJoinRoom() => StartCoroutine(JoinRoomCoroutine());
    private IEnumerator JoinRoomCoroutine()
    {
        var roomId = roomIdInput ? roomIdInput.text.Trim().ToUpper() : "";
        if (string.IsNullOrEmpty(roomId))
        {
            Log("RoomId를 입력해줘!");
            yield break;
        }

        Log("방 입장 요청 중...");

        var url = $"{serverHttpBase}/rooms/join";
        var reqObj = new JoinRoomRequest { roomId = roomId, clientId = clientId };
        var json = JsonUtility.ToJson(reqObj);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Log($"방 입장 실패: {req.error}");
            yield break;
        }

        var resp = JsonUtility.FromJson<JoinRoomResponse>(req.downloadHandler.text);
        if (resp == null || !resp.ok)
        {
            Log($"방 입장 실패: {(resp != null ? resp.error : "UNKNOWN")}");
            yield break;
        }

        currentRoomId = resp.roomId;
        inRoom = true;
        SetScreenRoom();

        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;

        Log($"roomId={resp.roomId}");
        RefreshRooms();

        yield return ConnectLobbyWs(resp.wsUrl);
    }

    // ---------------------------
    // Leave
    // ---------------------------
    private void OnLeaveRoom() => StartCoroutine(LeaveRoomCoroutine());
    private IEnumerator LeaveRoomCoroutine()
    {
        Log("방에서 나가는 중...");

        var closeTask = CloseWs();
        while (!closeTask.IsCompleted) yield return null;

        // 상태 초기화
        currentRoomId = "";
        inRoom = false;
        isHost = false;
        canStart = false;
        prevCanStart = false;
        prevPlayerCount = -1;

        createRoomButton.interactable = true;
        joinRoomButton.interactable = true;

        startGameButton.gameObject.SetActive(false);
        startGameButton.interactable = false;
        leaveRoomButton.gameObject.SetActive(false);

        Log(" ");
        RefreshRooms();
        SetScreenLobby();
    }

    // ---------------------------
    // WebSocket 연결
    // ---------------------------
    private IEnumerator ConnectLobbyWs(string wsUrl)
    {
        // 기존 연결 닫기
        var closeTask = CloseWs();
        while (!closeTask.IsCompleted) yield return null;

        Log("WebSocket 연결 중...");
        ws = new WebSocket(wsUrl);

        ws.OnOpen += () => Log("플레이어 기다리는 중...");
        ws.OnError += (e) => Log($"WS 에러: {e}");
        ws.OnClose += (e) => Log("WS 종료");

        ws.OnMessage += (bytes) =>
        {
            var json = Encoding.UTF8.GetString(bytes);
            Debug.Log("[WS] " + json);

            string type = ExtractString(json, "\"type\":", "");

            if (type == "joined")
            {
                // 서버가 joined에 isHost를 주는 경우: 즉시 반영
                // (host 승계는 room_state에 hostClientId가 올 때 더 정확)
                if (json.Contains("\"isHost\":true")) isHost = true;
            }
            else if (type == "room_state")
            {
                int newCount = ExtractInt(json, "\"playerCount\":", 0);
                bool newCanStart = json.Contains("\"canStart\":true");

                // hostClientId가 오는 서버라면 그걸로 호스트 판정 (너 코드가 이 방식이었지) :contentReference[oaicite:4]{index=4}
                string hostClientId = ExtractString(json, "\"hostClientId\":", "");
                if (!string.IsNullOrEmpty(hostClientId))
                    isHost = (hostClientId == clientId);

                // 2명 모였을 때 로그 바꾸기
                if (!prevCanStart && newCanStart)
                {
                    Log(isHost ? "2명 모임! [게임시작] 버튼을 눌러줘" : "2명 모임! 호스트가 시작 누르길 기다리는 중...");
                }
                else if (prevCanStart && !newCanStart)
                {
                    Log("플레이어 기다리는 중...");
                }
                else if (newCount != prevPlayerCount)
                {
                    Log($"현재 인원: {newCount}/2");
                }

                canStart = newCanStart;
                prevCanStart = newCanStart;
                prevPlayerCount = newCount;

                // 방 목록 텍스트도 최신화(겸사겸사)
                RefreshRooms();
            }
            else if (type == "game_start")
            {
                var m = JsonUtility.FromJson<GameStartMsg>(json);

                GameNetInfo.IsHost = isHost;
                GameNetInfo.HostIp = m.hostIp;
                GameNetInfo.Port = (ushort)(m.port == 0 ? 7777 : m.port);
                GameNetInfo.SceneName = string.IsNullOrEmpty(m.sceneName) ? "GameScene" : m.sceneName;

                Log($"게임 시작! hostIp={GameNetInfo.HostIp}");
                SceneManager.LoadScene(GameNetInfo.SceneName);
            }
            else if (type == "error")
            {
                // 서버가 HOST_IP_EMPTY 같은 에러 보내면 여기로 옴
                Log("서버 에러 메시지 수신: " + json);
            }
        };

        var connectTask = ws.Connect();
        while (!connectTask.IsCompleted) yield return null;
    }

    private async System.Threading.Tasks.Task CloseWs()
    {
        if (ws == null) return;
        try { await ws.Close(); } catch { }
        ws = null;
    }

    // ---------------------------
    // Start Game (Host only)
    // ---------------------------
    [Serializable]
    private class StartGameMsg
    {
        public string type;
        public string sceneName;
        public string hostIp;
    }

    private async void OnStartGame()
    {
        if (!inRoom) { Log("방에 먼저 들어가야 해!"); return; }
        if (!isHost) { Log("호스트만 시작할 수 있어!"); return; }
        if (!canStart) { Log("2명이 모여야 시작할 수 있어!"); return; }
        if (ws == null) { Log("WS 연결이 없어!"); return; }

        string myIp = GetLocalIPv4();
        if (string.IsNullOrEmpty(myIp))
        {
            Log("내 LAN IP를 못 찾았어. (와이파이/랜 연결 확인)");
            return;
        }

        var msg = new StartGameMsg
        {
            type = "start_game",
            sceneName = "GameScene",
            hostIp = myIp
        };

        Log($"게임 시작 요청 전송 (hostIp={myIp})");
        await ws.SendText(JsonUtility.ToJson(msg));
    }

    // ---------------------------
    // Mini JSON helpers (string/number)
    // ---------------------------
    private string ExtractString(string json, string key, string fallback)
    {
        int idx = json.IndexOf(key, StringComparison.Ordinal);
        if (idx < 0) return fallback;
        idx += key.Length;

        if (idx >= json.Length) return fallback;
        while (idx < json.Length && (json[idx] == ' ')) idx++;

        if (idx >= json.Length) return fallback;
        if (json[idx] != '"') return fallback;
        idx++;

        int end = json.IndexOf('"', idx);
        if (end < 0) return fallback;

        return json.Substring(idx, end - idx);
    }

    private int ExtractInt(string json, string key, int fallback)
    {
        int idx = json.IndexOf(key, StringComparison.Ordinal);
        if (idx < 0) return fallback;
        idx += key.Length;

        while (idx < json.Length && (json[idx] == ' ')) idx++;

        int end = idx;
        while (end < json.Length && char.IsDigit(json[end])) end++;

        var s = json.Substring(idx, end - idx);
        return int.TryParse(s, out int v) ? v : fallback;
    }

    private void ClearRoomButtons()
    {
        if (roomListContent == null) return;

        for (int i = roomListContent.childCount - 1; i >= 0; i--)
            Destroy(roomListContent.GetChild(i).gameObject);
    }

    private void BuildRoomButtons(RoomsResponse resp)
    {
        if (roomListContent == null || roomItemPrefab == null) return;

        ClearRoomButtons();

        if (resp == null || resp.rooms == null || resp.rooms.Length == 0)
        {
            // 방 없을 때: 텍스트 하나만 만들어도 되고 그냥 비워도 됨
            return;
        }

        foreach (var r in resp.rooms)
        {
            var go = Instantiate(roomItemPrefab, roomListContent);
            var ui = go.GetComponent<RoomItemUI>();
            if (ui == null) ui = go.AddComponent<RoomItemUI>();

            ui.Setup(this, r.roomId, r.playerCount, r.started);

            // 버튼 클릭 연결
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(ui.OnClickJoin);
            }
        }
    }

    private void SetScreenLobby()
    {
        if (lobbyPanel) lobbyPanel.SetActive(true);
        if (roomPanel) roomPanel.SetActive(false);

        // 로비 상태로 버튼 원복
        createRoomButton.interactable = true;
        joinRoomButton.interactable = true;

        startGameButton.gameObject.SetActive(false);
        leaveRoomButton.gameObject.SetActive(false);

        // 방 리스트 갱신 다시 켜기
        RefreshRooms();
    }

    private void SetScreenRoom()
    {
        if (lobbyPanel) lobbyPanel.SetActive(false);
        if (roomPanel) roomPanel.SetActive(true);

        // 방 안에서는 만들기/입장 못하게
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;

        // 방 패널 버튼은 Update에서 isHost/canStart/inRoom로 컨트롤해도 됨
        startGameButton.gameObject.SetActive(true);
        leaveRoomButton.gameObject.SetActive(true);
    }

    public void JoinRoomById(string roomId)
    {
        if (inRoom)
        {
            Log("이미 방 안에 있어. 나가기 후 입장해줘!");
            return;
        }

        if (roomIdInput != null)
            roomIdInput.text = roomId;

        StartCoroutine(JoinRoomCoroutine());
    }

    private void Log(string msg)
    {
        if (statusText) statusText.text = msg;
        Debug.Log("[Lobby] " + msg);
    }
}
