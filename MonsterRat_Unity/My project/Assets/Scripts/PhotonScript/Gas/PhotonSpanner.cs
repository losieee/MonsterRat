using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class PhotonSpanner : InvenBase
{
    public override ToolType Type => ToolType.Spanner;

    public float distance = 2f;
    public float fixTime = 5f;
    public Image fixGauge; // 여기에 민기님이 넣은 흰원 이미지 넣으면 될 듯 합니다.
    public AudioSource source;
    public AudioClip spannerSound;

    private float currentTime;
    private bool isRepairingSoundPlaying = false;

    public override void Tick()
    {
        if (Input.GetMouseButton(0))
        {
            if (interactor == null || interactor.cam == null)
            {
                ResetGauge();
                return;
            }

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
                if (fixGauge != null && !fixGauge.gameObject.activeSelf)
                    fixGauge.gameObject.SetActive(true);

                currentTime += Time.deltaTime;

                if (!isRepairingSoundPlaying)
                {
                    Rpc_StartSpannerSound();
                    isRepairingSoundPlaying = true;
                }

                if (currentTime <= fixTime)
                {
                    if (fixGauge != null)
                        fixGauge.fillAmount = currentTime / fixTime;
                }
                else
                {
                    valve.FixValve();
                    ResetGauge();
                }
            }
            else
            {
                // 광선이 관통한 모든 물체 중에 밸브가 아예 없으면 초기화
                ResetGauge();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetGauge();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_StartSpannerSound()
    {
        if (source == null || spannerSound == null)
            return;

        source.clip = spannerSound;
        source.loop = true;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        float baseVolume = HasInputAuthority ? 0.85f : 1f;
        float finalVolume = baseVolume * effectVolume;
        source.volume = finalVolume;

        if (!source.isPlaying)
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

    private void ResetGauge()
    {
        currentTime = 0f;
        if (fixGauge != null)
        {
            fixGauge.fillAmount = 0f;
            fixGauge.gameObject.SetActive(false); // UI 숨김
        }

        if (isRepairingSoundPlaying)
        {
            Rpc_StopSpannerSound();
            isRepairingSoundPlaying = false;
        }
    }
}