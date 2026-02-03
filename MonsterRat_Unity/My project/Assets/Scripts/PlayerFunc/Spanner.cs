using UnityEngine;
using UnityEngine.UI;

public class Spanner : InvenBase
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
                currentTime = 0f;
                fixGauge.fillAmount = 0f;
                return; 
            }

            float d = Vector3.Distance(interactor.transform.position, t.transform.position);
            if (d > distance)
            {
                currentTime = 0f;
                fixGauge.fillAmount = 0f;
                return;
            }

            if(t.layer == 14)
            {
                currentTime += Time.deltaTime;

                if (currentTime <= fixTime)
                {
                    fixGauge.fillAmount = currentTime / fixTime;
                }
                else
                {
                    Destroy(t.gameObject);
                    currentTime = 0f;
                    fixGauge.fillAmount = 0f;
                }
            }
            else
            {
                currentTime = 0f;
                fixGauge.fillAmount = 0f;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            currentTime = 0;
            fixGauge.fillAmount = 0;
        }
    }
}
