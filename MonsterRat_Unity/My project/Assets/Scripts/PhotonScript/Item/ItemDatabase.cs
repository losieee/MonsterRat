using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;
    public List<ItemData> allItems = new List<ItemData>();

    private void Awake()
    {
        // 싱글톤 및 씬 유지 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 게임 시작 시  ItemData를 불러오기
            //LoadAllItems();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadAllItems()
    {
        ItemData[] loadedItems = Resources.LoadAll<ItemData>("ItemData");

        // 리스트로 변환하여 저장
        allItems = loadedItems.ToList();
        
        Debug.Log($"[ItemDatabase] 총 {allItems.Count}개의 아이템 데이터를 성공적으로 불러왔습니다.");
    }

    // ID로 아이템 데이터 찾기 복구용입니다 
    public ItemData GetItem(int id)
    {
        ItemData foundItem = allItems.Find(x => x.itemID == id);

        if (foundItem == null)
        {
            Debug.LogError($"[ItemDatabase] ID가 {id}인 아이템을 찾을 수 없습니다! ItemData의 ID값을 확인하세요.");
        }

        return foundItem;
    }

    // 리스트에 있는 아이템을 인덱스로 가져오는 용도
    public ItemData GetItemByIndex(int index)
    {
        if (index < 0 || index >= allItems.Count)
            return null;

        return allItems[index];
    }
}