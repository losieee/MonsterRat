using UnityEngine;
using UnityEngine.UI;

public class PhotonSpanner : InvenBase
{
    public override ToolType Type => ToolType.Spanner;

    public float distance = 2f;
    public float fixTime = 5f;
    public Image fixGauge; // 여기에 민기님이 넣은 흰원 이미지 넣으면 될 듯 합니다.

    private float currentTime;

    public override void Tick()
    {
        

        // 좌클릭을 꾹 누르고 있을 때
        if (Input.GetMouseButton(0))
        {
            if (interactor == null) return;

            GameObject t = interactor.LookTarget;

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

            GasValveSync valve = t.GetComponentInParent<GasValveSync>();

            if (valve != null)
            {
                if (fixGauge != null && !fixGauge.gameObject.activeSelf)
                    fixGauge.gameObject.SetActive(true);

                currentTime += Time.deltaTime;

                // 게이지 채우기
                if (currentTime <= fixTime)
                {
                    if (fixGauge != null)
                        fixGauge.fillAmount = currentTime / fixTime;
                }
                else
                {
                    valve.FixValve();
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

    private void ResetGauge()
    {
        currentTime = 0f;
        if (fixGauge != null)
        {
            fixGauge.fillAmount = 0f;
            fixGauge.gameObject.SetActive(false); // UI 숨김
        }
    }
}