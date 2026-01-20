using UnityEngine;
using UnityEngine.UI;

public class GasGauge : MonoBehaviour
{
    public PlayerGas player;
    public Image fillImage;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerGas>();

        UpdateGauge();
    }

    void Update()
    {
        UpdateGauge();
    }

    void UpdateGauge()
    {
        if (player == null || fillImage == null) return;

        fillImage.fillAmount = player.GetNormalized();
    }
}
