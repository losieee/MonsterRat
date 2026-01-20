using UnityEngine;

public class PlayerGas : MonoBehaviour
{
    public float pollution = 0f;
    public float maxPollution = 100f;

    // 오염도 증가 함수
    public void AddExposure(float amount)
    {
        pollution += amount;
        pollution = Mathf.Clamp(pollution, 0f, maxPollution);
    }

    // 가스 게이지
    public float GetNormalized()
    {
        if (maxPollution <= 0f) return 0f;
        return pollution / maxPollution;
    }
}
