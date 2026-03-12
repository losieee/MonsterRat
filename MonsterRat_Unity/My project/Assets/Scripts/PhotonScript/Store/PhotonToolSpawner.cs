using UnityEngine;
using Fusion;

public class PhotonToolSpawner : NetworkBehaviour
{
    [Header("소환될 아이템들 (바닥용 껍데기 프리팹)")]
    // 주의: 플레이어 손에 들어가는 무기 스크립트가 아니라, 바닥에 굴러다니는 프리팹이어야 합니다!
    public NetworkPrefabRef[] tools; 

    [Header("아이템이 떨어질 위치 (상점 옆 배출구)")]
    public Transform itemSpawnPoint;

    // 상점 UI 버튼의 OnClick 이벤트에 이 함수를 연결해 주세요! (n은 0=대걸레, 1=총 등)
    public void BuyItem(int n)
    {
        if (Runner != null && itemSpawnPoint != null)
        {
            // 누가 버튼을 누르든, 방장에게 "배출구 위치에 n번 아이템을 생성해 줘!" 라고 요청합니다.
            RPC_RequestSpawnTool(itemSpawnPoint.position, itemSpawnPoint.rotation, n);
        }
    }

    // 오직 방장(서버)만 이 요청을 받아서 공식적으로 아이템을 생성합니다.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSpawnTool(Vector3 pos, Quaternion rot, int n)
    {
        if (n >= 0 && n < tools.Length)
        {
            // 방장이 생성했기 때문에, 이 아이템은 완벽한 '공유 객체'가 되어 모두에게 보입니다!
            Runner.Spawn(tools[n], pos, rot, null); // null = 이 아이템의 특정 주인은 없음(공공재)
        }
    }
}