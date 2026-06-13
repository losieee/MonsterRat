using UnityEngine;
using System;
using System.Collections;

public class TutorialMop : TutorialInvenBase
{
    public override TutorialToolType Type => TutorialToolType.Mop;

    public Animator anim;
    public float coolTime;
    bool canClean = true;

    public float cleanDistance = 5f;
    public Transform rayOrigin;
    public LayerMask ratLayer;

    public AudioSource source;
    public AudioClip mopSound;

    public override void Tick()
    {
        if (!canClean) return;
        if (Input.GetMouseButtonDown(0))
        {
            GameObject t = FindTargetIgnoringRat();
            if (t == null) return;

            if (t.layer == 6)
            {
                TutorialPollutionControl singlePol = t.GetComponentInParent<TutorialPollutionControl>();
                if (singlePol != null)
                {
                    singlePol.CleanOnce();
                    PlayMopSound();
                    StartCoroutine(Cooldown());
                }

            }

        }
    }

    GameObject FindTargetIgnoringRat()
    {
        Transform origin = rayOrigin;

        if (origin == null && Camera.main != null)
            origin = Camera.main.transform;

        if (origin == null)
            return null;

        Ray ray = new Ray(origin.position, origin.forward);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            cleanDistance,
            ~0,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
            return null;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;

            GameObject hitObj = hit.collider.gameObject;

            // Rat 레이어면 무시하고 뒤에 있는 오브젝트 계속 검사
            if (ratLayer != -1 && hitObj.layer == ratLayer)
                continue;

            return hitObj;
        }

        return null;
    }

    public void PlayMopSound()
    {
        if (source == null || mopSound == null)
            return;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        source.PlayOneShot(mopSound, effectVolume);
    }

    IEnumerator Cooldown()
    {
        canClean = false;
        anim.SetTrigger("SolCleaning");

        yield return new WaitForSeconds(coolTime);

        canClean = true;
    }
}