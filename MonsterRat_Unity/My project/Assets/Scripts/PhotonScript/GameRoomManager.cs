using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement; 

public class GameRoomManager : MonoBehaviourPunCallbacks
{
    public string playerPrefabName = "PhotonPlayer";
    public Transform[] spawnPoints;

    void Start()
    {
        // 네트워크에 연결되어 있고 방에 들어와 있는지 확인하기
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            SpawnPlayer();
        }
    }

    // 연결이 안 되어 있었다면 연결이 완료된 이 시점에 생성 시도
    public override void OnJoinedRoom()
    {
        // 내 캐릭터가 씬에 존재하는지 확인하는 간단한 방법
        if (GetMyPlayer() == null)
        {
            SpawnPlayer();
        }
    }

    void SpawnPlayer()
    {
        // 중복 방지
        if (GetMyPlayer() != null) return;

        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        int spawnIndex = (actorNumber - 1) % spawnPoints.Length;

        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            spawnIndex = spawnIndex % spawnPoints.Length;
            pos = spawnPoints[spawnIndex].position;
            rot = spawnPoints[spawnIndex].rotation;
        }
        PhotonNetwork.Instantiate(playerPrefabName, pos, rot);
    }

    // 이건 넣어놓으면 좋다고 해서 넣어놓긴 했습니다.
    GameObject GetMyPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine) return p;
        }
        return null;
    }
}