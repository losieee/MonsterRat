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
        Debug.Log($"[GameAutoStart] IsHost={GameNetInfo.IsHost}, HostIp='{GameNetInfo.HostIp}', Port={GameNetInfo.Port}");

        // 1) 포트 세팅 (호스트/클라 공통)
        if (tp != null)
        {
            tp.port = GameNetInfo.Port;
            Debug.Log($"[GameAutoStart] TelepathyTransport.port set to {tp.port}");
        }
        else
        {
            Debug.LogError("[GameAutoStart] TelepathyTransport 컴포넌트가 없음! NetworkManager 오브젝트에 붙어있는지 확인");
        }

        // 2) 호스트면 Host만 시작
        if (GameNetInfo.IsHost)
        {
            Debug.Log("[GameAutoStart] StartHost()");
            nm.StartHost();
        }
        // 3) 클라이언트면 IP 필수
        else
        {
            if (string.IsNullOrEmpty(GameNetInfo.HostIp))
            {
                Debug.LogError("[GameAutoStart] HostIp 비어있음. StartClient 중단");
                return;
            }
            nm.networkAddress = GameNetInfo.HostIp;
            Debug.Log($"[GameAutoStart] StartClient() -> {nm.networkAddress}:{GameNetInfo.Port}");
            nm.StartClient();
        }
    }
}
