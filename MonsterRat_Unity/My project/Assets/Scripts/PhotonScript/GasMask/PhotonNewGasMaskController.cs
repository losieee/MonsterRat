using Fusion;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PhotonNewGasMaskController : InvenBase
{
    public override ToolType Type => ToolType.NewGasMask;

    public Image gasMaskFill;
    public float coolTime = 20f;
    public float gasIgnoreTime = 10f;

    // 방독면을 넘겨줬을 때 쿨타임이 그대로 남아있어야 하기 때문에
    [Networked] private TickTimer gasIgnoreTimer { get; set; }
    [Networked] private TickTimer reuseTimer { get; set; }

    // 지금 방독면을 사용 중인가
    public bool UseMask
    {
        get
        {
            if (Runner == null) return false;
            return !gasIgnoreTimer.ExpiredOrNotRunning(Runner);     // gasIgnoreTimer가 아직 안끝났으면 true
        }
    }

    // 쿨타임 기다리는 중인가
    public bool IsCooldown
    {
        get
        {
            if (Runner == null) return false;
            return !reuseTimer.ExpiredOrNotRunning(Runner);
        }
    }

    private void Awake()
    {
        if (gasMaskFill.gameObject != null)
            gasMaskFill.gameObject.SetActive(false);

        if (gasMaskFill != null)
            gasMaskFill.fillAmount = 1f;
    }

    public override void OnSelect()
    {
        base.OnSelect();

        if (!HasInputAuthority)
        {
            HideGasMaskUI();
            return;
        }

        // 이미 사용 중이거나 쿨타임 중이면 다시 들었을 때 UI 표시
        if (UseMask || IsCooldown)
        {
            ShowGasMaskUI();
        }
    }

    public override void OnDeselect()
    {
        HideGasMaskUI();
        base.OnDeselect();
    }

    public override void Tick()
    {
        if (!HasInputAuthority) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryUseMask();
        }
    }

    private void Update()
    {
        if (!HasInputAuthority)
        {
            HideGasMaskUI();
            return;
        }

        UpdateGasMaskUI();
    }

    private bool IsOtherMaskBusy()
    {
        PhotonGasMaskController other = GetComponent<PhotonGasMaskController>();

        if (other == null) return false;

        return other.UseMask;
    }

    private void TryUseMask()
    {
        if (Runner == null) return;

        if (IsCooldown || IsOtherMaskBusy())
            return;

        if (HasStateAuthority)
        {
            StartGasMaskNetworked();
        }
        else
        {
            RPC_RequestUseGasMask();
        }
    }

    // 현재 방독면의 남은 재사용 시간 가져오기
    public float GetCooldownRemaining()
    {
        if (Runner == null) return 0f;

        float remain = reuseTimer.RemainingTime(Runner) ?? 0f;
        return Mathf.Max(0f, remain);
    }

    // 주운 방독면에 남은 쿨타임 적용
    public void ApplyCooldownRemaining(float remainingTime)
    {
        if (Runner == null) return;

        if (HasStateAuthority)
        {
            ApplyCooldownRemaining_Internal(remainingTime);
        }
        else
        {
            RPC_ApplyCooldownRemaining(remainingTime);
        }
    }

    // 방독면 사용 중 버렸을 때 기능 정지
    public void StopMaskEffectOnly()
    {
        if (Runner == null) return;

        HideGasMaskUI();

        if (HasStateAuthority)
        {
            StopMaskEffectOnly_Internal();
        }
        else
        {
            RPC_StopMaskEffectOnly();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_StopMaskEffectOnly()
    {
        StopMaskEffectOnly_Internal();
    }

    private void StopMaskEffectOnly_Internal()
    {
        if (!HasStateAuthority) return;

        // 가스 무시 효과만 즉시 끊음
        gasIgnoreTimer = TickTimer.None;

        // reuseTimer는 그대로 둠
        // 그래야 버린 아이템에는 남은 쿨타임을 저장할 수 있음
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ApplyCooldownRemaining(float remainingTime)
    {
        ApplyCooldownRemaining_Internal(remainingTime);
    }

    private void ApplyCooldownRemaining_Internal(float remainingTime)
    {
        if (!HasStateAuthority) return;

        remainingTime = Mathf.Max(0f, remainingTime);

        if (remainingTime <= 0f)
        {
            gasIgnoreTimer = TickTimer.None;
            reuseTimer = TickTimer.None;
            return;
        }

        // 주워온 아이템은 이미 사용 중이 아니라 쿨타임 중으로만 복원
        gasIgnoreTimer = TickTimer.None;
        reuseTimer = TickTimer.CreateFromSeconds(Runner, remainingTime);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestUseGasMask()
    {
        if (IsCooldown || IsOtherMaskBusy())
            return;

        StartGasMaskNetworked();
    }

    // 타이머 시작
    private void StartGasMaskNetworked()
    {
        if (!HasStateAuthority) return;

        gasIgnoreTimer = TickTimer.CreateFromSeconds(Runner, gasIgnoreTime);

        // 다시 사용 가능한 시간 = 사용 시간(10초) + 쿨타임 (20초)
        reuseTimer = TickTimer.CreateFromSeconds(Runner, gasIgnoreTime + coolTime);
    }

    private void UpdateGasMaskUI()
    {
        if (!enabled) return;
        if (Runner == null)
            return;

        bool isUsing = UseMask;
        bool isCooling = IsCooldown;

        if (isUsing || isCooling)
            ShowGasMaskUI();

        if (gasMaskFill == null)
            return;

        // 방독면 사용중 : 1 -> 0
        if (isUsing)
        {
            float remain = gasIgnoreTimer.RemainingTime(Runner) ?? 0f;
            gasMaskFill.fillAmount = Mathf.Clamp01(remain / gasIgnoreTime);
            return;
        }

        // 쿨타임 대기 : 0 -> 1
        if (isCooling)
        {
            float remain = reuseTimer.RemainingTime(Runner) ?? 0f;

            float cooldownRemain = Mathf.Clamp(remain, 0f, coolTime);

            gasMaskFill.fillAmount = Mathf.Clamp01(1f - (cooldownRemain / coolTime));
            return;
        }

        gasMaskFill.fillAmount = 1f;

        if (gasMaskFill.gameObject != null)
            gasMaskFill.gameObject.SetActive(false);
    }

    private void ShowGasMaskUI()
    {
        if (gasMaskFill != null)
            gasMaskFill.gameObject.SetActive(true);
    }

    public void HideGasMaskUI()
    {
        if (gasMaskFill.gameObject != null)
            gasMaskFill.gameObject.SetActive(false);
    }
}
