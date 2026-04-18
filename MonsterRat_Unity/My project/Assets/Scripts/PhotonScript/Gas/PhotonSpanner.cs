using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class PhotonSpanner : InvenBase
{
    public override ToolType Type => ToolType.Spanner;

    public float distance = 2f;
    public float fixTime = 5f;
    public Image fixGauge; // ø©±‚ø° πŒ±‚¥‘¿Ã ≥÷¿∫ »Úø¯ ¿ÃπÃ¡ˆ ≥÷¿∏∏È µ… µÌ «’¥œ¥Ÿ.
    public AudioSource source;
    public AudioClip spannerSound;

    private float currentTime;
    private bool isRepairingSoundPlaying = false;

    public override void Tick()
    {
        if (Input.GetMouseButton(0))
        {
            if (interactor == null)
            {
                ResetGauge();
                return;
            }

            if (!interactor.RaycastWorld(distance, out RaycastHit hit))
            {
                ResetGauge();
                return;
            }

            GasValveSync valve = hit.collider.GetComponentInParent<GasValveSync>();

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
        source.volume = HasInputAuthority ? 0.85f : 1f;

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
            fixGauge.gameObject.SetActive(false); // UI º˚±Ë

            if (source != null && source.isPlaying)
            {
                source.Stop();
            }
        }
    }
}