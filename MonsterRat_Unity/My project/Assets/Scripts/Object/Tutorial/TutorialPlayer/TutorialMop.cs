using UnityEngine;
using System.Collections;

public class TutorialMop : TutorialInvenBase
{
    public override TutorialToolType Type => TutorialToolType.Mop;

    public float coolTime = 1f;
    bool canClean = true;

    public override void Tick()
    {
        if (!canClean) return;

        if (!canClean) return;
        if (Input.GetMouseButtonDown(0))
        {
            GameObject t = interactor != null ? interactor.LookTarget : null;
            if (t == null) return;

            if (t.layer == 6)
            {
                TutorialPollutionControl singlePol = t.GetComponentInParent<TutorialPollutionControl>();
                if (singlePol != null)
                {
                    singlePol.CleanOnce();
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