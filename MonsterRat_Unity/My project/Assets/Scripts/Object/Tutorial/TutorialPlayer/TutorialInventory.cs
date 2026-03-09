using System;
using System.Collections.Generic;
using UnityEngine;

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
    public int currentSlot { get; private set; } = 1;

    [HideInInspector] public bool hasMop = false;
    [HideInInspector] public bool hasGun = false;
    [HideInInspector] public bool hasSpanner = false;

    List<TutorialToolType> slots = new List<TutorialToolType>();

    // 튜토리얼 전용 사용 잠금
    private HashSet<TutorialToolType> unlocked = new HashSet<TutorialToolType>();

    public event Action<TutorialToolType> OnToolAdded;

    void Awake()
    {
        slots.Clear();
        slots.Add(TutorialToolType.Hand);       // 손은 1슬롯 고정

        unlocked.Add(TutorialToolType.Hand);
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
        int idx = currentSlot - 1;
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

        return true;
    }

    // 슬롯 선택
    public void SelectSlot(int slot)
    {
        if (slot < 1 || slot > slots.Count) return;

        TutorialToolType want = slots[slot - 1];
        if (!IsUnlocked(want)) return;
        currentSlot = slot;
    }

    public IReadOnlyList<TutorialToolType> GetSlots() => slots;

    public bool HasAllTutorialTools() => hasMop && hasGun && hasSpanner;
}
