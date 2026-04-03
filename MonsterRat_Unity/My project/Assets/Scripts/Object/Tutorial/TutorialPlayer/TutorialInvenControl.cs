using UnityEngine;

public class TutorialInvenControl : MonoBehaviour
{
    public KeyCode handKey = KeyCode.BackQuote;
    public KeyCode slot1 = KeyCode.Alpha1;
    public KeyCode slot2 = KeyCode.Alpha2;
    public KeyCode slot3 = KeyCode.Alpha3;
    public KeyCode slot4 = KeyCode.Alpha4;

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
            if (Input.GetKeyDown(handKey)) inv.SelectHand();

            if (Input.GetKeyDown(slot1)) inv.SelectSlot(1);
            if (Input.GetKeyDown(slot2)) inv.SelectSlot(2);
            if (Input.GetKeyDown(slot3)) inv.SelectSlot(3);
            if (Input.GetKeyDown(slot4)) inv.SelectSlot(4);

            TutorialToolType want = inv.CurrentTool();
            if (current == null || current.Type != want)
                SwitchTo(want);
        }

        if (current != null)
            current.Tick();
    }

    void FixedUpdate()
    {
        if (ui != null && ui.IsUIOpen) return;

        if (current != null)
            current.FixedTick();
    }

    void SwitchTo(TutorialToolType type)
    {
        if (current != null)
            current.OnDeselect();

        current = null;

        foreach (var t in tools)
        {
            if (t.Type == type)
            {
                current = t;
                break;
            }
        }

        if (current != null)
            current.OnSelect();
    }
}