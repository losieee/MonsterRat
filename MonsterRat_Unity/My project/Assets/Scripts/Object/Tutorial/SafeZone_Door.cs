using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SafeZone_Door : MonoBehaviour
{
    Animator anim;

    public bool canWork = false;
    public GameObject btnOutLine;
    public GameObject clearDoorOutLine;
    public AudioSource source;
    public AudioClip clip;
    public AudioClip clear;
        
    bool canOpen = true;    

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void OpenDoor()
    {
        if (!canOpen || !canWork) return;

        canOpen = false;
        anim.SetTrigger("DoorOpen");

        PlayDoorSound();

        StartCoroutine(WaitCool(3f));
        btnOutLine.SetActive(false);
    }

    IEnumerator WaitCool(float val)
    {
        yield return new WaitForSeconds(val);
        canOpen = true;
    }

    public void OpenClearDoor()
    {
        anim.SetTrigger("ClearDoorOpen");

        PlayClearSound();

        clearDoorOutLine.SetActive(false);
        StartCoroutine(ClearTutorial());
    }

    IEnumerator ClearTutorial()
    {
        yield return new WaitForSeconds(2);
        TutorialScreenFader.Instance.FadeOut(0.5f);
        yield return new WaitForSeconds(0.5f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("ZZin_Main_Lobby");
    }

    void PlayDoorSound()
    {
        if (source == null || clip == null) return;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        source.clip = clip;
        source.loop = false;
        source.volume = 0.5f * effectVolume;
        source.time = 0f;
        source.Play();
    }

    void PlayClearSound()
    {
        if (source == null || clear == null) return;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        source.clip = clear;
        source.loop = false;
        source.volume = effectVolume;
        source.time = 0f;
        source.Play();
    }
}
