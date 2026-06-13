using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class GasGauge : MonoBehaviour
{
    public Image fillImage;
    public Image pollutionEffect;

    private PlayerGas player;

    public float alphaSmoothSpeed = 5f;

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

        // 오염 효과 유지
        if (normalized >= 0.25f)
            targetAlpha = 0.5f;

        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * alphaSmoothSpeed);
        pollutionEffect.color = c;
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