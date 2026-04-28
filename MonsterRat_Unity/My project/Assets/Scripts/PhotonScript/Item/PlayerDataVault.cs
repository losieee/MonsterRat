using System.Collections.Generic;
using Fusion;

public static class PlayerDataVault  // 이건 AI한테 한번 짜달라고 했습니다.
{
    // 플레이어별 소지 아이템 ID 리스트 저장 (PlayerRef 사용으로 클라/호스트 구분)
    private static Dictionary<PlayerRef, List<int>> savedInventories = new Dictionary<PlayerRef, List<int>>();

    // 데이터 존재 여부 확인용
    public static bool HasData(PlayerRef player) => savedInventories.ContainsKey(player);

    // 저장 로직
    public static void SaveInventory(PlayerRef player, List<int> itemIDs)
    {
        if (savedInventories.ContainsKey(player))
            savedInventories[player] = itemIDs;
        else
            savedInventories.Add(player, itemIDs);
    }

    // 불러오기 로직
    public static List<int> GetInventory(PlayerRef player)
    {
        if (savedInventories.ContainsKey(player))
        {
            var data = savedInventories[player];
            // 한 번 불러온 데이터는 삭제 (다음 씬에서 또 복구되지 않게)
            savedInventories.Remove(player);
            return data;
        }
        return null;
    }
}