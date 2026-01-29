using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;

// 게임 씬(함선)에서 캐릭터 스폰과 입력을 담당하는 매니저
public class GameRoomManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Player Prefab")]
    public NetworkObject playerPrefab; // 아까 만든 캡슐 프리팹을 여기에 넣으세요
    public Transform spawnPoint;
    private NetworkRunner _runner;

    void Start()
    {
        // 씬에 이미 존재하는 NetworkRunner를 찾아서 이 스크립트를 콜백에 등록
        // LobbyManager에서 생성된 Runner가 씬이 넘어가도 살아있습니다 (DontDestroyOnLoad 속성 때문)
        _runner = FindObjectOfType<NetworkRunner>();
        if (_runner != null)
        {
            _runner.AddCallbacks(this);
        }
    }

    // 플레이어가(나 자신 포함) 게임에 접속하면 호출됨
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // [수정됨] 숫자로 쓴 좌표 대신, 아까 만든 SpawnPoint의 위치를 사용
            Vector3 spawnPos;

            if (spawnPoint != null)
            {
                // 스폰 포인트가 있으면 그 위치 + 약간의 랜덤 (겹침 방지)
                spawnPos = spawnPoint.position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            }
            else
            {
                // 혹시 깜빡하고 스폰포인트 안 넣었으면 기본 위치 (0,2,0)
                spawnPos = new Vector3(0, 2, 0);
            }

            runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
        }
    }

    // 매 프레임 Fusion이 입력을 요구할 때 호출됨
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        // 유니티 입력 시스템에서 키 입력 가져오기
        data.moveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        data.lookRotation = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        // Fusion으로 전송
        input.Set(data);
    }

    // --- 필수 인터페이스들 (사용 안 함) ---
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
}