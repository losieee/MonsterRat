using UnityEngine;

public class PlayTimeManager : MonoBehaviour
{
    public static PlayTimeManager Instance;

    public float PlayTime { get; private set; }
    public bool IsCounting { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!IsCounting) return;

        PlayTime += Time.deltaTime;
    }

    public void StartCounting()
    {
        IsCounting = true;
    }

    public void StopCounting()
    {
        IsCounting = false;
    }

    public void ResetPlayTime()
    {
        PlayTime = 0f;
    }

    public void SetPlayTime(float time)
    {
        PlayTime = time;
    }
}