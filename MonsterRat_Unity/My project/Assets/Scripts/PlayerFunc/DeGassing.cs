using UnityEngine;

public class DeGassing : InvenBase
{
    public override ToolType Type => ToolType.DeGassing;

    public GasAbsorb gasAbsorb;

    public override void OnSelect()
    {
        // 도구 선택될 때 활성화
        if (gasAbsorb != null)
            gasAbsorb.enabled = true;
    }

    public override void OnDeselect()
    {
        // 도구 해제될 때 흡입 중지 + 비활성화
        if (gasAbsorb != null)
        {
            gasAbsorb.StopSuck();
            gasAbsorb.enabled = false;
        }
    }

    public override void Tick()
    {
        if (gasAbsorb == null) return;

        // 우클릭 누르고 있는 동안 흡입
        if (Input.GetMouseButton(1))
        {
            gasAbsorb.SuckTick(Time.deltaTime);
        }
        else
        {
            gasAbsorb.StopSuck();
        }
    }
}
