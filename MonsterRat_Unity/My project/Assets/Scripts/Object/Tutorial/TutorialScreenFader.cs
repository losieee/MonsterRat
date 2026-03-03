using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialScreenFader : MonoBehaviour
{
    public static TutorialScreenFader Instance;

    [SerializeField] CanvasGroup group;
    [SerializeField] float defaultDuration = 0.5f;

    Coroutine co;

    void Awake()
    {
        Instance = this;

        if (group == null) group = GetComponentInChildren<CanvasGroup>();

        // 처음은 투명
        //group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    public void FadeOut(float duration = -1f)
    {
        if (duration < 0f) duration = defaultDuration;
        StartFade(1f, duration, blockInput: true);
    }

    // 페이드 중 클릭 막기
    public void FadeIn(float duration = -1f)
    {
        if (duration < 0f) duration = defaultDuration;
        StartFade(0f, duration, blockInput: false);
    }

    public void StartFade(float targetAlpha, float duration, bool blockInput)
    {
        if (group == null) return;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(FadeRoutine(targetAlpha, duration, blockInput));
    }

    IEnumerator FadeRoutine(float target, float duration, bool blockInput)
    {
        // 페이드 중 클릭 막기
        group.blocksRaycasts = blockInput;
        group.interactable = false;

        float start = group.alpha;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            group.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }

        group.alpha = target;

        // 완전히 투명해졌으면 클릭 허용
        if (Mathf.Approximately(target, 0f))
        {
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        co = null;
    }
}
