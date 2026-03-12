using UnityEngine;
using Fusion;

public class PhotonToolSpawner : NetworkBehaviour
{
    [Header("소환될 아이템들 (바닥용 껍데기 프리팹)")]
    public NetworkPrefabRef[] tools; 

    [Header("아이템이 떨어질 위치 (상점 옆 배출구)")]
    public Transform itemSpawnPoint;

    public void BuyItem(int n)
    {
        if (Runner != null && itemSpawnPoint != null)
        {
            RPC_RequestSpawnTool(itemSpawnPoint.position, itemSpawnPoint.rotation, n);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSpawnTool(Vector3 pos, Quaternion rot, int n)
    {
        if (n >= 0 && n < tools.Length)
        {
            Runner.Spawn(tools[n], pos, rot, null); // null = 이 아이템의 특정 주인은 없음(공공재)
        }
    }
}