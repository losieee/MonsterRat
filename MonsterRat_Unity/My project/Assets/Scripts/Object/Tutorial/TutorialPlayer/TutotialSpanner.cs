using UnityEngine;
using UnityEngine.UI;

public class TutotialSpanner : TutorialInvenBase
{
    public override TutorialToolType Type => TutorialToolType.Spanner;

    public float distance = 2f;
    public float fixTime = 5f;
    public Image fixGauge;
    public Animator anim;

    float currentTime;
    bool isPlaying = false;

    public override void Tick()
    {
        if (Input.GetMouseButton(0))
        {
            GameObject t = interactor != null ? interactor.LookTarget : null;
            if (t == null)
            {
                ResetFix();
                return;
            }

            float d = Vector3.Distance(interactor.transform.position, t.transform.position);
            if (d > distance)
            {
                ResetFix();
                return;
            }

            if (t.layer == 14)
            {
                currentTime += Time.deltaTime;

                if (currentTime <= fixTime)
                {
                    fixGauge.fillAmount = currentTime / fixTime;
                    SpannerAnim(true);
                }
                else
                {
                    Destroy(t.gameObject);
                    ResetFix();
                }
            }
            else
            {
                ResetFix();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetFix();
        }
    }

    void ResetFix()
    {
        currentTime = 0f;

        if (fixGauge != null)
            fixGauge.fillAmount = 0f;

        SpannerAnim(false);
    }

    void SpannerAnim(bool play)
    {
        if (anim == null) return;
        if (isPlaying == play) return;

        isPlaying = play;
        anim.SetBool("isFixed", play);
    }
}
 