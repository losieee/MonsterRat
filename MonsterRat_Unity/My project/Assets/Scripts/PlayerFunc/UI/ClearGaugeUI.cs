using UnityEngine;
using UnityEngine.UI;

public class ClearGaugeUI : MonoBehaviour
{
    [SerializeField] private Image clearGaugeFill;
    [SerializeField] private Text clearGaugeText;

    private void Update()
    {
        if (clearGaugeFill == null)
            return;

        if (/*OnlyPresentation.Instance == null*/ ClearManager.Instance == null)
        {
            clearGaugeFill.fillAmount = 0f;

            if (clearGaugeText != null)
                clearGaugeText.text = "0%";

            return;
        }

        float fill = /*OnlyPresentation.Instance.ClearRatio01;*/ ClearManager.Instance.ClearRatio01;

        clearGaugeFill.fillAmount = fill;

        if (clearGaugeText != null)
            clearGaugeText.text = $"{fill * 100f:F0}%";
    }
}