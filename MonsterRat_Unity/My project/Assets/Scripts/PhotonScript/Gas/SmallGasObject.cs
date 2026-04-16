using System.Collections;
using UnityEngine;
using Fusion;

public class SmallGasObject : NetworkBehaviour
{
    [Header("Scale Settings")]
    public Vector3 originalScale = new Vector3(0.1f, 0.7f, 0.2f);

    [Networked]
    public float currentRatio { get; set; }

    private BoxCollider boxcol;

    private void Awake()
    {
        boxcol = GetComponent<BoxCollider>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            currentRatio = 1f;
        }
        transform.localScale = originalScale;

        // 클라이언트 레이캐스트 통과 버그 방지
        StartCoroutine(RefreshCollider());
    }

    IEnumerator RefreshCollider()
    {
        yield return null;
        if (boxcol != null)
        {
            boxcol.enabled = false;
            boxcol.enabled = true;
        }
    }

    public override void Render()
    {
        Vector3 targetScale = originalScale * currentRatio;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 15f);
    }

    //판단은 가스 스스로가 한다
    public void SuckGas(float amount)
    {
        if (Object == null || !Object.IsValid) return;

        if (Object.HasStateAuthority)
        {
            // 내가 방장이면 즉각 축소
            ApplyShrink(amount);
        }
        else
        {
            // 클라이언트면 방장에게 부탁
            RPC_ShrinkGas(amount);
        }
    }

    private void ApplyShrink(float amount)
    {
        currentRatio -= amount;
        if (currentRatio <= 0.333f)
        {
            Runner.Despawn(Object);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ShrinkGas(float amount)
    {
        ApplyShrink(amount);
    }
}