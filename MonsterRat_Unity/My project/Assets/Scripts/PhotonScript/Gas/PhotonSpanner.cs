using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class PhotonSpanner : InvenBase
{
    public override ToolType Type => ToolType.Spanner;

    public SpannerMiniGame spannerMiniGame;
    public SpannerMiniGameAimLab spannerMiniGameAimLab;

    public float distance = 2f;
    public AudioSource source;
    public AudioClip spannerSound;

    public override void Tick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (interactor == null || interactor.cam == null)
                return;

            // 기존 코드는 첫 번째로 맞은 물체만 검사해서 가스에 막히면 취소됨
            // if (!interactor.RaycastWorld(distance, out RaycastHit hit)) { ResetGauge(); return; }
            // GasValveSync valve = hit.collider.GetComponentInParent<GasValveSync>();

            // 레이캐스트 all을 사용해 광선이 뚫고 지나간 모든 물체를 검사하기
            Ray ray = new Ray(interactor.cam.position, interactor.cam.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            GasValveSync valve = null;

            // 맞은 모든 물체들을 하나씩 뒤져서 밸브가 있는지 확인
            foreach (RaycastHit hit in hits)
            {
                GasValveSync foundValve = hit.collider.GetComponentInParent<GasValveSync>();
                if (foundValve != null)
                {
                    valve = foundValve;
                    break; // 밸브를 찾았으면 끝
                }
            }

            if (valve != null)
            {
                bool normalPlaying = spannerMiniGame != null && spannerMiniGame.IsPlaying;
                bool aimLabPlaying = spannerMiniGameAimLab != null && spannerMiniGameAimLab.IsPlaying;

                if (normalPlaying || aimLabPlaying)
                    return;

                bool hasNormal = spannerMiniGame != null;
                bool hasAimLab = spannerMiniGameAimLab != null;

                if (hasNormal && hasAimLab)
                {
                    int randomGame = Random.Range(0, 2);

                    Debug.Log("선택된 미니게임: " + randomGame);

                    if (randomGame == 0)
                        spannerMiniGame.StartMiniGame(valve, this);
                    else
                        spannerMiniGameAimLab.StartMiniGame(valve, this);
                }
                else if (hasNormal)
                {
                    Debug.Log("일반 미니게임만 연결됨");
                    spannerMiniGame.StartMiniGame(valve, this);
                }
                else if (hasAimLab)
                {
                    Debug.Log("AimLab 미니게임만 연결됨");
                    spannerMiniGameAimLab.StartMiniGame(valve, this);
                }
                else
                {
                    Debug.LogWarning("스패너 미니게임이 둘 다 연결되지 않음");
                }
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_StartSpannerSound()
    {
        if (source == null || spannerSound == null)
            return;

        source.clip = spannerSound;
        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        float baseVolume = HasInputAuthority ? 0.85f : 1f;
        float finalVolume = baseVolume * effectVolume;
        source.volume = finalVolume;
        
        source.Play();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_StopSpannerSound()
    {
        if (source == null)
            return;

        if (source.isPlaying)
            source.Stop();
    }
}