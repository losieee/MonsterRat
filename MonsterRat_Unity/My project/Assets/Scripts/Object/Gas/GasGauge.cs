using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class GasGauge : MonoBehaviour
{
    public static GasGauge Instance;

    public Image fillImage;
    public Image pollutionEffect;
    public Image antidoteEffect;

    private PlayerGas player;

    [Header("Pollution Effect")]
    public float alphaSmoothSpeed = 5f;

    [Header("Antidote Effect")]
    public float antidoteTargetAlpha = 0.5f;
    public float antidoteFadeInTime = 0.15f;
    public float antidoteHoldTime = 0.4f;
    public float antidoteFadeOutTime = 0.8f;

    private Coroutine antidoteRoutine;

    void Awake()
    {
        Instance = this;

        SetImageAlpha(pollutionEffect, 0f);
        SetImageAlpha(antidoteEffect, 0f);
    }

    void OnEnable()
    {
        PlayerGas.OnLocalAntidoteEffectRequested += PlayAntidoteEffect;
    }

    void OnDisable()
    {
        PlayerGas.OnLocalAntidoteEffectRequested -= PlayAntidoteEffect;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (player == null)
            FindTargetPlayer();

        if (player == null || fillImage == null) return;

        if (!player.IsReadyForGauge)
            return;

        fillImage.fillAmount = player.GetNormalized();

        UpdatePollutionEffect(fillImage.fillAmount);
    }

    void UpdatePollutionEffect(float normalized)
    {
        if (pollutionEffect == null) return;

        Color c = pollutionEffect.color;

        float targetAlpha = 0f;

        if (normalized >= 0.25f)
            targetAlpha = 0.5f;

        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * alphaSmoothSpeed);
        pollutionEffect.color = c;
    }

    public void PlayAntidoteEffect()
    {
        if (antidoteEffect == null)
            return;

        if (!gameObject.activeInHierarchy)
            return;

        if (!antidoteEffect.gameObject.activeSelf)
            antidoteEffect.gameObject.SetActive(true);

        if (antidoteRoutine != null)
            StopCoroutine(antidoteRoutine);

        antidoteRoutine = StartCoroutine(AntidoteEffectRoutine());
    }

    IEnumerator AntidoteEffectRoutine()
    {
        yield return StartCoroutine(FadeImageAlpha(antidoteEffect, antidoteTargetAlpha, antidoteFadeInTime));

        yield return new WaitForSeconds(antidoteHoldTime);

        yield return StartCoroutine(FadeImageAlpha(antidoteEffect, 0f, antidoteFadeOutTime));

        antidoteRoutine = null;
    }

    IEnumerator FadeImageAlpha(Image img, float targetAlpha, float duration)
    {
        if (img == null)
            yield break;

        Color c = img.color;
        float startAlpha = c.a;
        float time = 0f;

        if (duration <= 0f)
        {
            SetImageAlpha(img, targetAlpha);
            yield break;
        }

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            SetImageAlpha(img, alpha);

            yield return null;
        }

        SetImageAlpha(img, targetAlpha);
    }

    void SetImageAlpha(Image img, float alpha)
    {
        if (img == null)
            return;

        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    void FindTargetPlayer()
    {
        PlayerGas[] allPlayers = FindObjectsOfType<PlayerGas>();

        foreach (var p in allPlayers)
        {
            if (p == null) continue;

            // 튜토리얼
            if (!p.useNetworkAuthority)
            {
                player = p;
                return;
            }

            // 인게임
            NetworkObject no = p.GetComponentInParent<NetworkObject>();
            if (no != null && no.IsValid && no.HasInputAuthority)
            {
                player = p;
                return;
            }
        }
    }
}