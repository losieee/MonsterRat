using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(TelepathyTransport))]
public class GameAutoStart : MonoBehaviour
{
    private NetworkManager nm;
    private TelepathyTransport tp;

    void Awake()
    {
        nm = GetComponent<NetworkManager>();
        tp = GetComponent<TelepathyTransport>();
    }

    void Start()
    {
        // 1) 포트 세팅 (호스트/클라 공통)
        if (tp != null)
            tp.port = GameNetInfo.Port;

        // 2) 호스트면 Host만 시작
        if (GameNetInfo.IsHost)
        {
            Debug.Log($"[GameAutoStart] StartHost() port={GameNetInfo.Port}");
            nm.StartHost();
            return;
        }

        // 3) 클라이언트면 IP 필수
        if (string.IsNullOrEmpty(GameNetInfo.HostIp))
        {
            Debug.LogError("[GameAutoStart] HostIp가 비어있음. 로비에서 game_start hostIp 파싱 실패!");
            return;
        }

        nm.networkAddress = GameNetInfo.HostIp;
        Debug.Log($"[GameAutoStart] StartClient() -> {nm.networkAddress}:{GameNetInfo.Port}");
        nm.StartClient();
    }
}
