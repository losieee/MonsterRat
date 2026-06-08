using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class GameRoomManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("플레이어 프리팹")]
    public NetworkPrefabRef playerPrefab;

    [Header("스폰 위치들")]
    public Transform[] spawnPoints;

    public override void Spawned()
    {
        if (SceneManager.GetActiveScene().name == "ZZin_Main_Lobby")
            return;

        if (Runner.IsServer)
        {
            foreach (PlayerRef player in Runner.ActivePlayers)
            {
                SpawnPlayer(player);
            }
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (SceneManager.GetActiveScene().name == "ZZin_Main_Lobby")
            return;

        if (Runner.IsServer)
        {
            SpawnPlayer(player);
        }
    }

    private void SpawnPlayer(PlayerRef player)
    {
        if (Runner.GetPlayerObject(player) != null) return;

        int spawnIndex = player.PlayerId % spawnPoints.Length;
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            pos = spawnPoints[spawnIndex].position;
            rot = spawnPoints[spawnIndex].rotation;
        }

        NetworkObject spawnedPlayer = Runner.Spawn(playerPrefab, pos, rot, player);
        Runner.SetPlayerObject(player, spawnedPlayer);


      //  if (HostMigrationManager.IsMigrating) return; 이거 일단 비활성화

        if (Runner.GetPlayerObject(player) != null) return;
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Runner.IsServer)
        {
            NetworkObject playerObj = Runner.GetPlayerObject(player);
            if (playerObj != null)
            {
                Runner.Despawn(playerObj);
                Debug.Log($"플레이어({player.PlayerId}) 퇴장. 캐릭터를 삭제");
            }
        }
    }
}