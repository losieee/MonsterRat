using NUnit.Framework.Internal.Filters;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TutorialToolType
{
    Hand,           // 맨손
    Gun,            // 총
    Mop,            // 대걸레
    Spanner,        // 스패너
}
public class TutorialInventory : MonoBehaviour
{
    public int maxSlots = 5;
    public int CurrentSlot { get; private set; } = 0;

    [HideInInspector] public bool hasMop = false;
    [HideInInspector] public bool hasGun = false;
    [HideInInspector] public bool hasSpanner = false;
    [HideInInspector] public bool clickMop = false;
    [HideInInspector] public bool clickGun = false;
    [HideInInspector] public bool clickSpanner = false;

    [Header("인벤 이미지")]
    public GameObject invenPanel;
    public Image[] slotImages;
    public GameObject[] selectImages;
    public Sprite emptySlotSprite;
    public Sprite handSprite;
    public Sprite gunSprite;
    public Sprite mopSprite;
    public Sprite spannerSprite;

    [Header("아이템 모델")]
    public GameObject solModel;
    public GameObject gunModel;
    public GameObject spannerModel;

    [Header("아이템 드롭")]
    public Transform dropPoint;
    public GameObject gunPrefab;
    public GameObject mopPrefab;
    public GameObject spannerPrefab;

    List<TutorialToolType> slots = new List<TutorialToolType>();

    // 튜토리얼 전용 사용 잠금
    private HashSet<TutorialToolType> unlocked = new HashSet<TutorialToolType>();

    public event Action<TutorialToolType> OnToolAdded;

    void Awake()  
    {
        slots.Clear();
        slots.Add(TutorialToolType.Hand);       // 손은 1슬롯 고정

        unlocked.Clear();
        unlocked.Add(TutorialToolType.Hand);

        CurrentSlot = 0;
    }

    void Start()
    {
        if (invenPanel != null)
            invenPanel.SetActive(false);

        RefreshInventoryUI();
        UpdateSolModel();
        UpdateGunModel();
        UpdateSpannerModel();
        UpdateSelectImages(-1);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();
        if(Input.GetKeyDown(KeyCode.G))
            DropCurrentToolToWorld();
    }

    void UpdateSelectImages(int activeIndex)
    {
        if (selectImages == null || selectImages.Length == 0) return;

        for (int i = 0; i < selectImages.Length; i++)
        {
            if (selectImages[i] == null) continue;
            selectImages[i].SetActive(i == activeIndex);
        }
    }

    public void ToggleInventory()
    {
        if (invenPanel == null) return;
        invenPanel.SetActive(!invenPanel.activeSelf);
    }

    public void UnlockTool(TutorialToolType tool)
    {
        unlocked.Add(tool);
    }

    public bool IsUnlocked(TutorialToolType tool)
    {
        return unlocked.Contains(tool);
    }

    public TutorialToolType CurrentTool()
    {
        if (CurrentSlot == 0)
            return TutorialToolType.Hand;

        int idx = CurrentSlot;
        if (idx < 0 || idx >= slots.Count) return TutorialToolType.Hand;
        return slots[idx];
    }

    public bool HasTool(TutorialToolType tool)
    {
        if (tool == TutorialToolType.Hand) return true;
        return slots.Contains(tool);
    }

    public bool AddTool(TutorialToolType tool)
    {
        // 맨손이면 취소
        if (tool == TutorialToolType.Hand) return false;

        // 중복 구매 방지
        if (HasTool(tool)) return false;

        // 슬롯이 꽉 찼으면 추가 불가
        if (slots.Count >= maxSlots) return false;

        slots.Add(tool);

        if (tool == TutorialToolType.Mop) hasMop = true;
        else if (tool == TutorialToolType.Gun) hasGun = true;
        else if (tool == TutorialToolType.Spanner) hasSpanner = true;

        OnToolAdded?.Invoke(tool);

        RefreshInventoryUI();
        UpdateSolModel();
        UpdateGunModel();
        UpdateSpannerModel();

        return true;
    }

    public void SelectHand()
    {
        CurrentSlot = 0;
        UpdateSolModel();
        UpdateGunModel();
        UpdateSpannerModel();
        RefreshInventoryUI();

        UpdateSelectImages(-1);
    }

    // 슬롯 선택
    public void SelectSlot(int slot)
    {
        if (slot < 1 || slot >= slots.Count) return;

        TutorialToolType want = slots[slot];
        if (!IsUnlocked(want)) return;

        CurrentSlot = slot;

        UpdateSolModel();
        UpdateGunModel();
        UpdateSpannerModel();
        RefreshInventoryUI();

        UpdateSelectImages(slot - 1);
    }

    public bool DropCurrentToolToWorld()
    {
        TutorialToolType tool = CurrentTool();
        if (tool == TutorialToolType.Hand) return false;
        if (dropPoint == null) return false;

        GameObject prefab = GetPrefabByTool(tool);
        if (prefab == null) return false;

        Instantiate(prefab, dropPoint.position, dropPoint.rotation);

        RemoveCurrentTool();
        return true;
    }

    private void RemoveCurrentTool()
    {
        if (CurrentSlot == 0) return;
        if (CurrentSlot < 0 || CurrentSlot >= slots.Count) return;

        TutorialToolType removedTool = slots[CurrentSlot];
        slots.RemoveAt(CurrentSlot);

        CurrentSlot = 0;

        RefreshInventoryUI();
        UpdateSolModel();
        UpdateGunModel();
        UpdateSpannerModel();
    }

    public IReadOnlyList<TutorialToolType> GetSlots() => slots;

    public bool HasAllTutorialTools() => hasMop && hasGun && hasSpanner;

    void RefreshInventoryUI()
    {
        if (slotImages == null || slotImages.Length == 0) return;

        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] == null) continue;

            int toolIndex = i + 1;

            if (toolIndex < slots.Count)
            {
                Sprite sprite = GetSpriteByTool(slots[toolIndex]);
                slotImages[i].sprite = sprite != null ? sprite : emptySlotSprite;
                slotImages[i].enabled = true;
            }
            else
            {
                slotImages[i].sprite = emptySlotSprite;
                slotImages[i].enabled = true;
            }
        }
    }

    void UpdateSolModel()
    {
        if (solModel == null) return;
        solModel.SetActive(CurrentTool() == TutorialToolType.Mop);
    }

    void UpdateGunModel()
    {
        if (gunModel == null) return;
        gunModel.SetActive(CurrentTool() == TutorialToolType.Gun);
    }

    void UpdateSpannerModel()
    {
        if (spannerModel == null) return;
        spannerModel.SetActive(CurrentTool() == TutorialToolType.Spanner);
    }

    Sprite GetSpriteByTool(TutorialToolType tool)
    {
        switch (tool)
        {
            case TutorialToolType.Hand:
                return handSprite;
            case TutorialToolType.Gun:
                return gunSprite;
            case TutorialToolType.Mop:
                return mopSprite;
            case TutorialToolType.Spanner:
                return spannerSprite;
            default:
                return handSprite;
        }
    }
    GameObject GetPrefabByTool(TutorialToolType tool)
    {
        switch (tool)
        {
            case TutorialToolType.Gun:
                return gunPrefab;
            case TutorialToolType.Mop:
                return mopPrefab;
            case TutorialToolType.Spanner:
                return spannerPrefab;
            default:
                return null;
        }
    }
}
