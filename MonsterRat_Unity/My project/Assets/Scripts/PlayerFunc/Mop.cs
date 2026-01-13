using System.Collections;
using UnityEngine;

public class Mop : InvenBase
{
    public override ToolType Type => ToolType.Mop;

    public float coolTime = 1f;

    bool canClean = true;

    public override void Tick()
    {
        if (!canClean) return;
        if (Input.GetMouseButtonDown(0))
        {
            GameObject t = interactor != null ? interactor.LookTarget : null;
            if (t == null) return;

            if (t.layer == 6)
            {
                PollutionControl pol = t.GetComponentInParent<PollutionControl>();
                if (pol != null)
                {
                    pol.CleanOnce();
                    StartCoroutine(Cooldown());
                }
            }
        }
    }

    IEnumerator Cooldown()
    {
        canClean = false;
        yield return new WaitForSeconds(coolTime);
        canClean = true;
    }
}
