using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeleteObject : MonoBehaviour
{
    Animator anim;
    public HashSet<GameObject> deleteBoxes = new HashSet<GameObject>();

    public AudioSource source;
    public AudioClip fireSound;

    bool isDeleting = false;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void CanDelete()
    {
        if (isDeleting) return;
        isDeleting = true;

        anim.SetTrigger("PushButton");

        StartCoroutine(CloseDoor(2f));
    }

    IEnumerator CloseDoor(float delay)
    {
        yield return new WaitForSeconds(delay);

        var copy = new List<GameObject>(deleteBoxes);

        
        foreach (var box in copy)
        {
            if (box == null) continue;
            Destroy(box.transform.root.gameObject);
        }

        deleteBoxes.Clear();

        PlayFireSound();

        yield return new WaitForSeconds(3);

        StopFireSound();

        isDeleting = false;
    }

    void PlayFireSound()
    {
        if (source == null || fireSound == null) return;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        source.clip = fireSound;
        source.loop = true;
        source.volume = 0.5f * effectVolume;
        source.time = 0f;
        source.Play();
    }

    void StopFireSound()
    {
        if (source == null) return;

        source.Stop();
        source.loop = false;
    }
}
