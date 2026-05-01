using UnityEngine;
using System.Collections;

public enum ClearTargetType
{
    Pollution = 0,  // 얼룩
    Trash = 1,      // 쓰레기
    Gas = 2         // 배관
}

public class ClearBox : MonoBehaviour, IClearTarget
{
    public ClearTargetType clearType;

    public float weight = 1f;
    public float Remain01 => 1f;
    public float Weight => Mathf.Max(0f, weight);

    private bool _registered;       // 중복 등록 방지

    void OnEnable()
    {
        StartCoroutine(ApplyWeightAndRegisterRoutine());
    }

    // 처음 시작할때만 검사하면 등록이 안될 수 있음
    // 매 프레임 검사 (하다가 등록되면 멈춤)
    IEnumerator ApplyWeightAndRegisterRoutine()
    {
        while (StageProgressManager.Instance == null)
            yield return null;

        weight = StageProgressManager.Instance.GetWeight(clearType);

        // 치트 모드일때만 OnlyPresentation
        //while (OnlyPresentation.Instance == null)
        //yield return null;

        while (ClearManager.Instance == null)
            yield return null;

        if (!_registered)
        {
            //OnlyPresentation.Instance.Register(this);
            ClearManager.Instance.Register(this);
            _registered = true;
        }
    }

    void OnDestroy()
    {
        if (_registered && /*OnlyPresentation.Instance != null*/ ClearManager.Instance != null)
        {
            //OnlyPresentation.Instance.Unregister(this);
            ClearManager.Instance.Unregister(this);
        }
    }
}