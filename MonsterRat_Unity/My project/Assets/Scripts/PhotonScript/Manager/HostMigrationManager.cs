using UnityEngine;
using Fusion;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Fusion.Sockets;

public class HostMigrationManager : MonoBehaviour, INetworkRunnerCallbacks  //후에 스크립트를 확인할 민기쟝을 위해 주석을 조금 남겨놓습니다.
                                                                                // 스크립트의 대체적인 원리로는 호스트가 나가면 로비씬에 이 스크립트를 Runner가 없는 곳에 생성하고
{                                                                               // 게임씬까지 따라옵니다. 게임씬에서 호스트가 나가면, 플레이어가 나감과 동시에 클라이언트 플레이어도 삭제되는데 이건 삭제됨과 또 동시에 클라이언트의 위치에 
                                                                                // 새로운 캐릭터가 스폰이 되는 식으로 해놓았습니다. 
                                                                                // 이건 AI도 모르고해서 제가 블로그를 보면서 이런식으로 조금씩 짜면서 만든 기능이라 아마 많이 미흡할겁니다. 참고하십쇼.
    public static HostMigrationManager Instance;
    public static bool IsMigrating = false;

    [Header("마이그레이션 전용 플레이어 프리팹")] // 이걸로 다시 스폰할겁니다
    public NetworkPrefabRef playerPrefab;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void RegisterRunner(NetworkRunner runner)
    {
        runner.AddCallbacks(this);
    }

    public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("호스트 연결 끊김. 마이그레이션을 시작합니다...");
        IsMigrating = true;

        //물리적 위치를 저장
        Vector3 backupPos = Vector3.zero;
        Quaternion backupRot = Quaternion.identity;

        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            // 현재 켜져 있는 내 카메라 위치 백업
            if (pc.myCamObj != null && pc.myCamObj.activeInHierarchy)
            {
                backupPos = pc.transform.position;
                backupRot = pc.transform.rotation;
                Debug.Log("위치 백업 완료");
                break;
            }
        }

        //기존 통신망 종료 (캐릭터는 다 삭제됩니다)
        await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

        GameObject newRunnerObj = new GameObject("MigratedNetworkRunner");
        DontDestroyOnLoad(newRunnerObj);

        var newRunner = newRunnerObj.AddComponent<NetworkRunner>();
        newRunner.ProvideInput = true;
        newRunner.AddCallbacks(this);

        //매니저를 찾아 재활용합니다 라고 이건 AI가 알려줬어요
        // (이게 연결되어 있어야 GameRoomManager가 정상적으로 깨어납니다)
        var sceneManager = FindAnyObjectByType<NetworkSceneManagerDefault>();
        if (sceneManager == null) sceneManager = newRunnerObj.AddComponent<NetworkSceneManagerDefault>();

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            HostMigrationToken = hostMigrationToken,
            SceneManager = sceneManager
        };

        var result = await newRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log("마이그레이션 성공! 이제 내가 방장(Host)입니다.");
            newRunner.SessionInfo.IsOpen = true;
            newRunner.SessionInfo.IsVisible = true;

            var currentScene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
            NetworkObject[] sceneObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
            newRunner.RegisterSceneObjects(currentScene, sceneObjects, default);

            await Task.Delay(500); // 0.5초 대기

            bool foundMyPlayer = false;
            foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (pc.Object != null && pc.Object.HasInputAuthority)
                {
                    pc.transform.position = backupPos;
                    pc.transform.rotation = backupRot;
                    if (pc.myCamObj != null) pc.myCamObj.SetActive(true);
                    var listener = pc.GetComponentInChildren<AudioListener>(true);
                    if (listener != null) listener.enabled = true;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    foundMyPlayer = true;
                    break;
                }
            }

            //GameRoomManager가 비활성화 되어있으면 migration 매니저가 직접 스폰
            if (!foundMyPlayer)
            {
                Debug.LogWarning("GameRoomManager가 파괴되서 migration 매니저가 직접 스폰");

                // playerPrefab이 제대로 들어있는지 확인!
                if (playerPrefab.IsValid)
                {
                    // 백업해둔 위치(backupPos, backupRot)에 생성
                    NetworkObject myNewChar = newRunner.Spawn(playerPrefab, backupPos, backupRot, newRunner.LocalPlayer);
                    newRunner.SetPlayerObject(newRunner.LocalPlayer, myNewChar);

                    PlayerController pc = myNewChar.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        // 잃어버린 시야와 소리를 복구합니다 // 근데 왜 이건 이따구로 하는진 모르겠음
                        if (pc.myCamObj != null) pc.myCamObj.SetActive(true);
                        var listener = pc.GetComponentInChildren<AudioListener>(true);
                        if (listener != null) listener.enabled = true;

                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }
        }

        IsMigrating = false; // 복구 완료
    }

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
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigrationCleanUp(NetworkRunner runner) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}