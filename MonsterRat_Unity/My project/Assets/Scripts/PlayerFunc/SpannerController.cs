using UnityEngine;
using UnityEngine.UI;

public class SpannerController : InvenBase
{
    public override ToolType Type => ToolType.Spanner;

    public float distance = 2f;
    public float fixTime = 5f;
    public Image fixGauge;

    float currentTime;

    public override void Tick()
    {
        if (Input.GetMouseButton(0))
        {
            GameObject t = interactor != null ? interactor.LookTarget : null;
            if (t == null)
            {
                ResetGauge();
                return;
            }

            float d = Vector3.Distance(interactor.transform.position, t.transform.position);
            if (d > distance)
            {
                ResetGauge();
                return;
            }

            if (t.layer == 14)
            {
                currentTime += Time.deltaTime;

                if (currentTime <= fixTime)
                {
                    if (fixGauge != null)
                        fixGauge.fillAmount = currentTime / fixTime;
                }
                else
                {
                    Destroy(t.gameObject);
                    ResetGauge();
                }
            }
            else
            {
                ResetGauge();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetGauge();
        }
    }

    void OnDisable()
    {
        ResetGauge();
    }

    void OnDestroy()
    {
        ResetGauge();
    }

    private void ResetGauge()
    {
        currentTime = 0f;

        if (fixGauge != null)
            fixGauge.fillAmount = 0f;
    }
}
