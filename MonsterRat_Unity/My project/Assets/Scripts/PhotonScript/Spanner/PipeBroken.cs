using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PipeBroken : MonoBehaviour, IPointerClickHandler
{
    public Image crackImage;

    public AudioSource source;
    public AudioClip clip;

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
        PlaySound();

        yield return new WaitForSeconds(1f);
        mat.SetFloat("_Reveal", 0.3f);
        PlaySound();

        yield return new WaitForSeconds(1f);
        mat.SetFloat("_Reveal", 0.5f);
        PlaySound();

        if (!clicked)
        {
            manager.Missed();
        }

        Destroy(gameObject);
    }

    void PlaySound()
    {
        float effectVolume = 1f;
        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;
        source.volume = effectVolume;

        source.PlayOneShot(clip);
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
