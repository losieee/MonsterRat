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

        if (Input.GetMouseButtonDown(0))
        {
            if (interactor == null)
            {
                return;
            }

            GameObject t = interactor.LookTarget;

            Vector3 rayStart = Camera.main != null ? Camera.main.transform.position : transform.position;

            if (t == null)
            {
                if (Camera.main != null)
                    Debug.DrawRay(rayStart, Camera.main.transform.forward * 3f, Color.red, 2f);
                return;
            }

            Debug.DrawLine(rayStart, t.transform.position, Color.green, 2f);

            if (t.layer == 6)
            {
                PhotonPollutionControl multiPol = t.GetComponentInParent<PhotonPollutionControl>();
                if (multiPol != null)
                {
                    multiPol.CleanOnce();
                    StartCoroutine(Cooldown());
                    return;
                }

                PollutionControl singlePol = t.GetComponentInParent<PollutionControl>();
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