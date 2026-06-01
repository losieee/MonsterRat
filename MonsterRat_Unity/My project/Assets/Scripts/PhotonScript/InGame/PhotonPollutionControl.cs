using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PhotonPollutionControl : NetworkBehaviour, IClearTarget
{
    public Material[] polstep;
    MeshRenderer render;
    BoxCollider boxcol;

    //  변수로 만들어서 방에 있는 모든 사람이 똑같은 청소 횟수를 공유
    [Networked]
    public int cleanCount { get; set; }

    // 내 화면에서 이전 프레임의 청소 횟수를 기억하는 변수 
    private int _lastCleanCount = -1;

    [SerializeField] private float weight = 1f;

    public float Remain01 => 1f;
    public float Weight => Mathf.Max(0f, weight);

    void Awake()
    {
        render = GetComponent<MeshRenderer>();
        boxcol = GetComponent<BoxCollider>();
    }

    // 대신 퓨전의 Spawned를 사용 (중간에 접속한 사람도 처리하기 위함)
    public override void Spawned()
    {
        ApplyWeightFromStageProgress();

        UpdateMaterial();

        if (PollutionSpawner.Instance != null && Object != null)
            PollutionSpawner.Instance.RegisterSpawnedPollution(Object);

        StartCoroutine(RegisterWhenManagerReady());
        StartCoroutine(RefreshCollider());
    }

    void ApplyWeightFromStageProgress()
    {
        if (StageProgressManager.Instance == null)
            return;

        weight = StageProgressManager.Instance.GetWeight(ClearTargetType.Pollution);
    }

    IEnumerator RegisterWhenManagerReady()
    {
        while (/*OnlyPresentation.Instance == null*/ ClearManager.Instance == null)
            yield return null;

        //OnlyPresentation.Instance.Register(this);
        ClearManager.Instance.Register(this);
    }

    IEnumerator RefreshCollider()
    {
        yield return null;

        if(boxcol != null)
        {
            boxcol.enabled = false;
            boxcol.enabled = true;
        }
    }

    // 대걸레 스크립트(TutorialMop)가 마우스 클릭 시 이 함수를 실행 
    public void CleanOnce(int amount)
    {
        if (Object == null)
        {
            Debug.LogError($"[PhotonPollutionControl] Object is null: {name}");
            return;
        }

        // 내가 방장이면 바로 청소를 진행하고 게스트면 방장에게 부탁 
        if (Object.HasStateAuthority)
        {
            ApplyClean(amount);
        }
        else
        {
            RPC_RequestClean(amount);
        }
    }

    // 게스트가 방장에게 보내는 청소신호
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestClean(int amount)
    {
        ApplyClean(amount);
    }

    private void ApplyClean(int amount)
    {
        cleanCount += amount;

        if (cleanCount >= 3)
        {
            Runner.Despawn(Object);
        }
    }

    public override void Render()
    {
        if (cleanCount != _lastCleanCount)
        {
            UpdateMaterial();
            _lastCleanCount = cleanCount;
        }
    }

    private void UpdateMaterial()
    {
        if (render == null) return;
        if (polstep == null || polstep.Length == 0) return;

        int index = Mathf.Clamp(cleanCount, 0, polstep.Length - 1);
        render.material = polstep[index];
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (PollutionSpawner.Instance != null && Object != null)
            PollutionSpawner.Instance.UnregisterSpawnedPollution(Object);

        /*if (OnlyPresentation.Instance != null)
            OnlyPresentation.Instance.Unregister(this);*/
        
        if (ClearManager.Instance != null)
            ClearManager.Instance.Unregister(this);
    }
}