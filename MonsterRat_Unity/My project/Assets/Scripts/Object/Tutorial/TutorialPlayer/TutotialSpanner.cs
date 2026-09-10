using UnityEngine;
using UnityEngine.UI;

public class TutotialSpanner : TutorialInvenBase
{
    public override TutorialToolType Type => TutorialToolType.Spanner;

    public TutorialSpannerMiniGame spannerMiniGame;
    public float distance = 2f;

    public Animator anim;

    public AudioSource source;
    public AudioClip spannerSound;

    bool isPlaying = false;

    public override void Tick()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteractDoor();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (interactor == null || interactor.cam == null)
                return;

            Ray ray = new Ray(interactor.cam.position, interactor.cam.forward);
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );

            GameObject target = null;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject.layer == 14)
                {
                    target = hit.collider.gameObject;
                    break;
                }

                if (hit.collider.transform.parent != null &&
                    hit.collider.transform.parent.gameObject.layer == 14)
                {
                    target = hit.collider.transform.parent.gameObject;
                    break;
                }
            }

            if (target != null)
            {
                if (spannerMiniGame != null)
                {
                    if (spannerMiniGame.IsPlaying)
                        return;

                    spannerMiniGame.StartMiniGame();
                }
            }
        }
    }

    void TryInteractDoor()
    {
        if (interactor == null) return;

        if (interactor.RaycastWorld(distance, out RaycastHit hit))
        {
            int layer = hit.collider.gameObject.layer;

            if (layer == 10)
            {
                DeleteObject btn = hit.collider.GetComponent<DeleteObject>();
                if (btn != null) btn.CanDelete();
            }
            else if (layer == 17)
            {
                SafeZone_Door btn = hit.collider.GetComponentInParent<SafeZone_Door>();
                if (btn != null) btn.OpenDoor();
            }
            else if (layer == 18)
            {
                SafeZone_Door btn = hit.collider.GetComponentInParent<SafeZone_Door>();
                if (btn != null) btn.OpenClearDoor();
            }
        }
    }

    public void PlaySpannerSound()
    {
        if (source == null || spannerSound == null)
            return;

        source.clip = spannerSound;
        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        source.volume = effectVolume;

        source.Play();
    }

    void SpannerAnim(bool play)
    {
        if (anim == null) return;
        if (isPlaying == play) return;

        isPlaying = play;
        anim.SetBool("isFixed", play);
    }
}
