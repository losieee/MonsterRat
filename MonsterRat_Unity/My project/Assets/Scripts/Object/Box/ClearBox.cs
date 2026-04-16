using UnityEngine;
using System.Collections;

public class ClearBox : MonoBehaviour, IClearTarget
{
    public float weight = 1f;
    public float Remain01 => 1f;
    public float Weight => Mathf.Max(0f, weight);

    private bool _registered;       // 중복 등록 방지

    void OnEnable()
    {
        StartCoroutine(TryRegisterRoutine());
    }

    // 처음 시작할때만 검사하면 등록이 안될 수 있음
    // 매 프레임 검사 (하다가 등록되면 멈춤)
    IEnumerator TryRegisterRoutine()
    {
        while (!_registered)
        {
            if (OnlyPresentation.Instance != null)
            {
                OnlyPresentation.Instance.Register(this);
                _registered = true;
                yield break;
            }

            yield return null;
        }
    }

    void OnDestroy()
    {
        if (_registered && OnlyPresentation.Instance != null)
        {
            OnlyPresentation.Instance.Unregister(this);
        }
    }
}