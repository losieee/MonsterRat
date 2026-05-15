using UnityEngine;
using System.Collections;

public class TutorialMop : TutorialInvenBase
{
    public override TutorialToolType Type => TutorialToolType.Mop;

    public Animator anim;
    public float coolTime;
    bool canClean = true;

    public AudioSource source;
    public AudioClip mopSound;

    public override void Tick()
    {
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
                    PlayMopSound();
                    StartCoroutine(Cooldown());
                }

            }

        }
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