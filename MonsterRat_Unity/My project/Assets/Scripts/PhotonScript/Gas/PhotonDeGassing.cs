using UnityEngine;
using Fusion;
using System.Collections;

public class PhotonDeGassing : InvenBase
{
    public override ToolType Type => ToolType.DeGassing;

    public float distance = 5f;
    public float shrinkSpeed = 1.2f;
    public LayerMask gasLayerMask;
    public LayerMask pipeGasLayerMask;
    public AudioSource source;
    public AudioClip degassingClip;
    public float fadeOutTime = 0.25f;

    private Coroutine fadeCoroutine;
    private PipeSmallGasObject currentGas;
    private bool isHoldingSound;

    public override void Tick()
    {
        if (!HasInputAuthority) return;

        if (SlimUI.ModernMenu.UISettingsManager.isMenuOpen || PhotonPlayerUIState.isGlobalStoreOpen)
            return;

        if (interactor == null) return;
        if (interactor.cam == null) return;

        PipeSmallGasObject targetPipeGas = null;

        if (Input.GetMouseButton(0))
        {
            Ray ray = new Ray(interactor.cam.position, interactor.cam.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, distance, gasLayerMask, QueryTriggerInteraction.Collide))
            {
                SmallGasObject targetGas = hit.collider.GetComponentInParent<SmallGasObject>();

                if (targetGas != null)
                {
                    targetGas.SuckGas(Time.deltaTime * shrinkSpeed);
                }
            }

            if (Physics.Raycast(ray, out RaycastHit pipeHit, distance, pipeGasLayerMask, QueryTriggerInteraction.Collide))
            {
                targetPipeGas = pipeHit.collider.GetComponentInParent<PipeSmallGasObject>();
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            isHoldingSound = true;
            Rpc_StartGasCleanerSound();
        }

        if (Input.GetMouseButtonUp(0))
        {
            isHoldingSound = false;
            Rpc_StopGasCleanerSound();
        }

        if (targetPipeGas != currentGas)
        {
            if (currentGas != null)
                currentGas.PlayGas();

            currentGas = targetPipeGas;

            if (currentGas != null)
                currentGas.StopGas();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (currentGas != null)
            {
                currentGas.PlayGas();
                currentGas = null;
            }
        }
    }

    public override void OnDeselect()
    {
        if (!HasInputAuthority) return;

        isHoldingSound = false;
        StopDegassingLocalState();
        Rpc_StopGasCleanerSound();
    }

    private void OnDisable()
    {
        StopDegassingLocalState();

        if (source != null && source.isPlaying)
            source.Stop();
    }

    private void StopDegassingLocalState()
    {
        isHoldingSound = false;

        if (currentGas != null)
        {
            currentGas.PlayGas();
            currentGas = null;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_StartGasCleanerSound()
    {
        if (source == null || degassingClip == null)
            return;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        source.Stop();
        source.clip = degassingClip;
        source.loop = true;
        source.volume = effectVolume;
        source.Play();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_StopGasCleanerSound()
    {
        if (source == null)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOutGasSound());
    }

    private IEnumerator FadeOutGasSound()
    {
        float startVolume = source.volume;
        float time = 0f;

        while (time < fadeOutTime)
        {
            if (isHoldingSound)
            {
                fadeCoroutine = null;
                yield break;
            }

            time += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, time / fadeOutTime);
            yield return null;
        }

        source.Stop();
        source.volume = startVolume;
        fadeCoroutine = null;
    }
}