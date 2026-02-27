using UnityEngine;


[CreateAssetMenu(fileName = "New Item Data", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("아이템 정보")]
    public int itemID;
    public string itemName;

    [Header("인벤 정보")]
    public Sprite itemIcon;

    [Header("옵젝 정보")]
    public GameObject itemPrefab;
}