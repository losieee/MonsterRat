using UnityEngine;

public class TutorialInvenControl : MonoBehaviour
{
    [Header("Slot Keys")]
    public KeyCode slot1 = KeyCode.Alpha1;
    public KeyCode slot2 = KeyCode.Alpha2;
    public KeyCode slot3 = KeyCode.Alpha3;
    public KeyCode slot4 = KeyCode.Alpha4;
    public KeyCode slot5 = KeyCode.Alpha5;

    TutorialInventory inv;
    PlayerUIState ui;
    PlayerRaycast interactor;

    TutorialInvenBase[] tools;
    TutorialInvenBase current;

    void Awake()
    {
        inv = GetComponent<TutorialInventory>();
        ui = GetComponent<PlayerUIState>();
        interactor = GetComponent<PlayerRaycast>();
        tools = GetComponents<TutorialInvenBase>();

        // 도구 초기화
        foreach (var t in tools)
        {
            t.Init(ui, interactor);
            t.OnDeselect();
        }

        SwitchTo(inv != null ? inv.CurrentTool() : TutorialToolType.Hand);
    }

    void Update()
    {
        if (ui != null && ui.IsUIOpen) return;

        if (inv != null)
        {
            if (Input.GetKeyDown(slot1)) inv.SelectSlot(1);
            if (Input.GetKeyDown(slot2)) inv.SelectSlot(2);
            if (Input.GetKeyDown(slot3)) inv.SelectSlot(3);
            if (Input.GetKeyDown(slot4)) inv.SelectSlot(4);
            if (Input.GetKeyDown(slot5)) inv.SelectSlot(5);

            // 인벤의 현재 슬롯 도구가 바뀌면 도구 스위치
            TutorialToolType want = inv.CurrentTool();
            if (current == null || current.Type != want)
                SwitchTo(want);
        }

        // 현재 도구만 동작
        if (current != null)
            current.Tick();
    }

    void FixedUpdate()
    {
        if (ui != null && ui.IsUIOpen) return;

        if (current != null)
            current.FixedTick();
    }

    // 현재에 맞는 도구를 찾아서 활성화
    void SwitchTo(TutorialToolType type)
    {
        // 기존 도구 비활성화
        if (current != null) current.OnDeselect();
        current = null;

        // 같은 타입의 도구 찾기
        foreach (var t in tools)
        {
            if (t.Type == type)
            {
                current = t;
                break;
            }
        }

        // 찾으면 활성화
        if (current != null)
        {
            current.OnSelect();
        }
    }
}
