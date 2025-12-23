using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    private int spawnIndex = 0;

    public override Transform GetStartPosition()
    {
        // 씬에 있는 NetworkStartPosition들이 startPositions에 자동 등록됨
        if (startPositions == null || startPositions.Count == 0)
        {
            Debug.LogWarning("[MyNetworkManager] startPositions가 비어있음 (NetworkStartPosition이 씬에 있어야 함)");
            return null;
        }

        // 1번째 플레이어는 0번, 2번째 플레이어는 1번 (그 다음은 순환)
        Transform t = startPositions[spawnIndex % startPositions.Count];
        spawnIndex++;
        return t;
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);

        // 사람이 나가면 다시 0부터(다음 접속자가 1번 자리부터 시작하게)
        // 원하면 이 줄을 지워서 “계속 순환”하게 할 수도 있음.
        spawnIndex = 0;
    }
}
