using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class GasGauge : MonoBehaviour
{
    public Image fillImage;
    private PlayerGas player;

    void Update()
    {
        if (player == null)
            FindLocalPlayer();

        if (player == null || fillImage == null) return;

        fillImage.fillAmount = player.GetNormalized();
    }

    void FindLocalPlayer()
    {
        PlayerGas[] allPlayers = FindObjectsOfType<PlayerGas>();

        foreach (var p in allPlayers)
        {
            NetworkObject no = p.GetComponentInParent<NetworkObject>();
            if (no != null && no.HasInputAuthority)
            {
                player = p;
                break;
            }
        }
    }
}