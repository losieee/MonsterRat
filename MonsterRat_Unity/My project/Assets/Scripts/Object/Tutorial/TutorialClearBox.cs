using UnityEngine;

public class TutorialClearBox : MonoBehaviour, TutorialIClearTarget
{
    public float weight = 1f;
    public float Remain01 => 1f;
    public float Weight => Mathf.Max(0f, weight);

    void Start()
    {
        if (TutorialClearManager.Instance != null)
            TutorialClearManager.Instance.Register(this);
    }

    void OnDestroy()
    {
        if (TutorialClearManager.Instance != null)
            TutorialClearManager.Instance.Unregister(this);
    }
}
