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

        fillImage.fillAmount = player.GetNormalized();

        UpdatePollutionEffect(fillImage.fillAmount);
    }

    void UpdatePollutionEffect(float normalized)
    {
        if (pollutionEffect == null) return;

        Color c = pollutionEffect.color;
        float targetAlpha = Mathf.Lerp(0f, 0.8f, normalized);
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * alphaSmoothSpeed);
        pollutionEffect.color = c;
    }

    void FindTargetPlayer()
    {
        PlayerGas[] allPlayers = FindObjectsOfType<PlayerGas>();

        foreach (var p in allPlayers)
        {
            NetworkObject no = p.GetComponentInParent<NetworkObject>();
            if (no != null && no.HasInputAuthority)
            {
                player = p;
                return;
            }
        }

        if (allPlayers.Length > 0)
            player = allPlayers[0];
    }
}