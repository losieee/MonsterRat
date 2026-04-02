using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;  

public class PhotonPollutionControl : NetworkBehaviour, IClearTarget
{
    public Material[] polstep;
    MeshRenderer render;

    //  변수로 만들어서 방에 있는 모든 사람이 똑같은 청소 횟수를 공유
    [Networked]
    public int cleanCount { get; set; }

    // 내 화면에서 이전 프레임의 청소 횟수를 기억하는 변수 
    private int _lastCleanCount = -1;

    public float Remain01 => 1f;
    public float Weight => 1f;

    void Awake()
    {
        render = GetComponent<MeshRenderer>();
    }

    // 대신 퓨전의 Spawned를 사용 (중간에 접속한 사람도 처리하기 위함)
    public override void Spawned()
    {
        if (ClearManager.Instance != null)
            ClearManager.Instance.Register(this);
        UpdateMaterial();
    }

    // 대걸레 스크립트(TutorialMop)가 마우스 클릭 시 이 함수를 실행 
    public void CleanOnce()
    {
        // 내가 방장이면 바로 청소를 진행하고 게스트면 방장에게 부탁 
        if (Object.HasStateAuthority)
        {
            ApplyClean();
        }
        else
        {
            RPC_RequestClean();
        }
    }

    // 게스트가 방장에게 보내는 청소신호
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestClean()
    {
        ApplyClean();
    }

    private void ApplyClean()
    {
        cleanCount++;

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
        if (ClearManager.Instance != null)
            ClearManager.Instance.Unregister(this);
    }
}