using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PipeBroken : MonoBehaviour, IPointerClickHandler
{
    public Image crackImage;

    private Material mat;
    private SpannerMiniGameAimLab manager;
    private bool clicked = false;

    public void Init(SpannerMiniGameAimLab gameManager)
    {
        manager = gameManager;
    }

    void Start()
    {
        mat = crackImage.material;
        mat.SetFloat("_Reveal", 0f);

        StartCoroutine(RevealRoutine());
    }

    IEnumerator RevealRoutine()
    {
        yield return new WaitForSeconds(1f);
        mat.SetFloat("_Reveal", 0.15f);

        yield return new WaitForSeconds(1f);
        mat.SetFloat("_Reveal", 0.3f);

        yield return new WaitForSeconds(1f);
        mat.SetFloat("_Reveal", 0.5f);

        if (!clicked)
        {
            manager.Missed();
        }

        Destroy(gameObject);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clicked) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            clicked = true;
            manager.AddCount();
            Destroy(gameObject);
        }
    }
}
