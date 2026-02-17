using System.Collections.Generic;
using UnityEngine;

public enum ToolType
{
    Hand,           // 맨손
    Gun,            // 총
    Mop,            // 대걸레
    DeGassing,      // 가스제거기
    Spanner,        // 스패너
    Cutter,         // 절단기
    Flame,          // 화염방사기
}

public class Inventory : MonoBehaviour
{
    public int maxSlots = 5;
    public int currentSlot { get; private set; } = 1;

    [HideInInspector] public bool hasMop = false;
    [HideInInspector] public bool hasGun = false;
    [HideInInspector] public bool hasSpanner = false;

    List<ToolType> slots = new List<ToolType>();

    // 튜토리얼 전용 사용 잠금
    private HashSet<ToolType> unlocked = new HashSet<ToolType>();

    void Awake()
    {
        slots.Clear();
        slots.Add(ToolType.Hand);       // 손은 1슬롯 고정

        unlocked.Add(ToolType.Hand);
    }

    public void UnlockTool(ToolType tool)
    {
        unlocked.Add(tool);
    }

    public bool IsUnlocked(ToolType tool)
    {
        return unlocked.Contains(tool);
    }

    public ToolType CurrentTool()
    {
        int idx = currentSlot - 1;
        if (idx < 0 || idx >= slots.Count) return ToolType.Hand;
        return slots[idx];
    }

    public bool HasTool(ToolType tool)
    {
        if (tool == ToolType.Hand) return true;
        return slots.Contains(tool);
    }

    public bool AddTool(ToolType tool)
    {
        // 맨손이면 취소
        if (tool == ToolType.Hand) return false;

        // 중복 구매 방지
        if (HasTool(tool)) return false;

        // 슬롯이 꽉 찼으면 추가 불가
        if (slots.Count >= maxSlots) return false;

        slots.Add(tool);
        return true;
    }

    // 슬롯 선택
    public void SelectSlot(int slot)
    {
        if (slot < 1 || slot > slots.Count) return;

        ToolType want = slots[slot - 1];
        if (!IsUnlocked(want)) return;
        currentSlot = slot;
    }

    public IReadOnlyList<ToolType> GetSlots() => slots;
}
