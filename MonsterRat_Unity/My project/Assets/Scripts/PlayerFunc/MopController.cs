using System.Collections;
using UnityEngine;

public class MopController : InvenBase
{
    public override ToolType Type => ToolType.Mop;

    public float coolTime = 1f;
    bool canClean = true;

    public override void Tick()
    {
        if (!canClean) return;
        if (Input.GetMouseButtonDown(0))
        {
            if (interactor == null)
            {
                return;
            }

            GameObject t = interactor.LookTarget;

            Vector3 rayStart = Camera.main != null ? Camera.main.transform.position : transform.position;

            Debug.Log($"[Mop] hit point = {t.name}");

            if (t == null)
            {
                return;
            }

            if (t.layer == 6)
            {
                Debug.Log($"[Mop] hit = {t.name}");

                PhotonPollutionControl multiPol = t.GetComponentInParent<PhotonPollutionControl>();
                if (multiPol != null)
                {
                    multiPol.CleanOnce();
                    StartCoroutine(Cooldown());
                    return;
                }

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
