using UnityEngine;
using Fusion;
using System.Collections;
using System.Collections.Generic;

public class PhotonRePollutionControl : NetworkBehaviour
{
    // 얼룩 생성 시 여기에 저장
    private static readonly List<PhotonRePollutionControl> pollutionList = new List<PhotonRePollutionControl>();

    private static bool allClearedTriggered = false;

    public Material[] polstep;
    MeshRenderer render;
    BoxCollider boxcol;

    //  변수로 만들어서 방에 있는 모든 사람이 똑같은 청소 횟수를 공유
    [Networked]
    public int cleanCount { get; set; }

    // 내 화면에서 이전 프레임의 청소 횟수를 기억하는 변수 
    private int _lastCleanCount = -1;

    void Awake()
    {
        render = GetComponent<MeshRenderer>();
        boxcol = GetComponent<BoxCollider>();
    }

    // 생성
    public override void Spawned()
    {
        UpdateMaterial();

        if (!pollutionList.Contains(this))
        {
            pollutionList.Add(this);
        }

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

    // 대걸레 스크립트(TutorialMop)가 마우스 클릭 시 이 함수를 실행 
    public void CleanOnce(int amount)
    {
        if (Object == null)
            return;

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
            RePollutionSpawner.Instance.UnregisterPollution(Object);
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

    // 제거
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        RemovePollution();
    }

    private void OnDestroy()
    {
        RemovePollution();
    }

    void RemovePollution()
    {
        CheckAllPollutionCleared();
    }

    // 전체 제거 확인
    static void CheckAllPollutionCleared()
    {
        if (allClearedTriggered)
            return;

        if (pollutionList.Count > 0)
            return;

        allClearedTriggered = true;

        OnAllPollutionCleared();
    }

    // 전부 제거 됐을 때
    static void OnAllPollutionCleared()
    {
        
    }
}