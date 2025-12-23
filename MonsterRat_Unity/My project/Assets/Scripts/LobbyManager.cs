using System;
using System.Collections;
using System.Text;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("Server")]
    public string serverHttpBase = "http://127.0.0.1:3000"; // 다른 PC에서는 호스트PC IP로 바꿔야 함

    [Header("UI")]
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button startGameButton;
    public Button leaveRoomButton;
    public InputField roomIdInput;
    public Text statusText;
    public Text roomsText; // 선택

    private WebSocket ws;

    private string clientId;
    private string currentRoomId = "";
    private bool isHost = false;
    private bool canStart = false;
    private bool prevCanStart = false;
    private int prevPlayerCount = -1;

    // ---------------------------
    // Unity 시작
    // ---------------------------
    private void Start()
    {
        clientId = GetClientId();
        Log($" ");

        startGameButton.gameObject.SetActive(false);
        startGameButton.interactable = false;
        leaveRoomButton.gameObject.SetActive(false);
        leaveRoomButton.onClick.AddListener(OnLeaveRoom);

        createRoomButton.onClick.AddListener(OnCreateRoom);
        joinRoomButton.onClick.AddListener(OnJoinRoom);
        startGameButton.onClick.AddListener(OnStartGame);

        // 방 목록 주기 갱신(선택)
        InvokeRepeating(nameof(RefreshRooms), 1f, 2f);
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        ws?.DispatchMessageQueue();
#endif
        // 버튼 표시/활성화
        startGameButton.gameObject.SetActive(isHost);
        startGameButton.interactable = isHost && canStart;
    }

    private void OnDestroy()
    {
        _ = CloseWs();
    }

    // ---------------------------
    // 1) clientId 생성/저장
    // ---------------------------
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
    // 2) 방 만들기
    // ---------------------------
    [Serializable] private class CreateRoomRequest { public string clientId; }
    [Serializable] private class CreateRoomResponse { public bool ok; public string roomId; public string hostKey; public string wsUrl; public string error; }

    private void OnCreateRoom()
    {
        StartCoroutine(CreateRoomCoroutine());
    }

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
        if (!resp.ok)
        {
            Log($"방 생성 실패: {resp.error}");
            yield break;
        }

        currentRoomId = resp.roomId;
        isHost = true;
        Log($"방 생성 성공! roomId={resp.roomId}");

        // 버튼 실수 방지: 방 안에 있으면 create/join 비활성
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
        leaveRoomButton.gameObject.SetActive(true);

        // wsUrl로 WebSocket 연결
        yield return ConnectLobbyWs(resp.wsUrl);
    }

    // ---------------------------
    // 3) 방 입장
    // ---------------------------
    [Serializable] private class JoinRoomRequest { public string roomId; public string clientId; }
    [Serializable] private class JoinRoomResponse { public bool ok; public string roomId; public string wsUrl; public string error; }

    private void OnJoinRoom()
    {
        StartCoroutine(JoinRoomCoroutine());
    }

    private IEnumerator JoinRoomCoroutine()
    {
        var roomId = (roomIdInput != null) ? roomIdInput.text.Trim().ToUpper() : "";
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
        if (!resp.ok)
        {
            Log($"방 입장 실패: {resp.error}");
            yield break;
        }

        currentRoomId = resp.roomId;
        isHost = false;
        Log($"방 입장 성공! roomId={resp.roomId}");

        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
        leaveRoomButton.gameObject.SetActive(true);

        yield return ConnectLobbyWs(resp.wsUrl);
    }

    private void OnLeaveRoom()
    {
        StartCoroutine(LeaveRoomCoroutine());
    }

    private IEnumerator LeaveRoomCoroutine()
    {
        Log("방에서 나가는 중...");

        // WS 끊기
        var closeTask = CloseWs();
        while (!closeTask.IsCompleted) yield return null;

        // 로컬 상태 초기화
        currentRoomId = "";
        isHost = false;
        canStart = false;

        // UI 초기화
        startGameButton.gameObject.SetActive(false);
        startGameButton.interactable = false;

        leaveRoomButton.gameObject.SetActive(false);

        createRoomButton.interactable = true;
        joinRoomButton.interactable = true;

        Log(" ");
    }

    // ---------------------------
    // WebSocket 연결/메시지 처리
    // ---------------------------
    private IEnumerator ConnectLobbyWs(string wsUrl)
    {
        // 기존 연결 닫기
        var closeTask = CloseWs();
        while (!closeTask.IsCompleted) yield return null;

        Log("WebSocket 연결 중...");
        ws = new WebSocket(wsUrl);

        ws.OnOpen += () =>
        {
            Log("플레이어 기다리는 중...");
        };

        ws.OnError += (e) =>
        {
            Log($"WS 에러: {e}");
        };

        ws.OnClose += (e) =>
        {
            Log(" ");
        };

        ws.OnMessage += (bytes) =>
        {
            var json = Encoding.UTF8.GetString(bytes);
            Debug.Log("[WS] " + json);

            // joined: isHost 동기화(서버가 알려줌)
            if (json.Contains("\"type\":\"joined\""))
            {
                isHost = json.Contains("\"isHost\":true");
            }
            // room_state: 인원/시작 가능 여부 동기화
            else if (json.Contains("\"type\":\"room_state\""))
            {
                canStart = json.Contains("\"canStart\":true");

                // hostClientId를 기준으로 호스트 판정(호스트 승계 대응)
                string hostClientId = ExtractString(json, "\"hostClientId\":", "");
                isHost = (!string.IsNullOrEmpty(hostClientId) && hostClientId == clientId);

                // 2명 모였을 때 로그
                if (canStart)
                {
                    if (isHost) Log("버튼을 눌러 게임 시작");
                    else Log("시작을 기다리는 중...");
                }
            }
            // game_start: 둘 다 씬 이동
            else if (json.Contains("\"type\":\"game_start\""))
            {
                // 최소 구현: sceneName이 GameScene이라고 가정
                // (정석은 JSON 파싱해서 sceneName 읽기)
                UnityMainThread(() =>
                {
                    Log("게임 시작! 씬 이동");
                    _ = CloseWs();
                    SceneManager.LoadScene("GameScene");
                });
            }
        };

        var connectTask = ws.Connect();
        while (!connectTask.IsCompleted) yield return null;
    }

    private int ExtractInt(string json, string key, int fallback)
    {
        int idx = json.IndexOf(key, StringComparison.Ordinal);
        if (idx < 0) return fallback;
        idx += key.Length;

        // 숫자 끝까지 읽기
        int end = idx;
        while (end < json.Length && char.IsDigit(json[end])) end++;

        var numStr = json.Substring(idx, end - idx);
        return int.TryParse(numStr, out int v) ? v : fallback;
    }

    private async System.Threading.Tasks.Task CloseWs()
    {
        if (ws == null) return;
        try { await ws.Close(); } catch { }
        ws = null;
    }

    private string ExtractString(string json, string key, string fallback = "")
    {
        int idx = json.IndexOf(key, StringComparison.Ordinal);
        if (idx < 0) return fallback;
        idx += key.Length;

        // key 다음이 " 로 시작한다고 가정
        if (idx >= json.Length || json[idx] != '"') return fallback;
        idx++;

        int end = json.IndexOf('"', idx);
        if (end < 0) return fallback;

        return json.Substring(idx, end - idx);
    }

    // ---------------------------
    // Start 버튼(호스트만)
    // ---------------------------
    private async void OnStartGame()
    {
        if (!isHost)
        {
            Log("호스트만 시작할 수 있어!");
            return;
        }
        if (!canStart)
        {
            Log("2명이 모여야 시작할 수 있어!");
            return;
        }
        if (ws == null)
        {
            Log("WS 연결이 없어!");
            return;
        }

        Log("게임 시작 요청 전송");
        await ws.SendText("{\"type\":\"start_game\",\"sceneName\":\"GameScene\"}");
    }

    // ---------------------------
    // 방 목록 표시(선택)
    // ---------------------------
    [Serializable] private class RoomsResponse { public RoomInfo[] rooms; }
    [Serializable] private class RoomInfo { public string roomId; public long createdAt; public int playerCount; public bool started; }

    private void RefreshRooms()
    {
        StartCoroutine(RefreshRoomsCoroutine());
    }

    private IEnumerator RefreshRoomsCoroutine()
    {
        var url = $"{serverHttpBase}/rooms";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) yield break;

        if (roomsText != null)
        {
            var resp = JsonUtility.FromJson<RoomsResponse>(req.downloadHandler.text);
            if (resp?.rooms == null) yield break;

            var sb = new StringBuilder();
            foreach (var r in resp.rooms)
            {
                sb.AppendLine($"{r.roomId} ({r.playerCount}/2)");
            }
            roomsText.text = sb.ToString();
        }
    }

    // ---------------------------
    // 유틸 출력
    // ---------------------------
    private void Log(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log("[Lobby] " + msg);
    }

    private void UnityMainThread(Action a)
    {
        // 지금은 OnMessage가 메인스레드에서 오는 경우가 많아 바로 실행해도 되지만,
        // 안전하게 메인스레드 실행이 필요하면 Dispatcher를 쓰면 됨.
        a?.Invoke();
    }
}
