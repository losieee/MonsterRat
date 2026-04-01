using UnityEngine;
using Fusion;
public class PhotonInvenBase : NetworkBehaviour
{
    protected PlayerUIState ui;
    protected PlayerRaycast interactor;

    public virtual ToolType Type => ToolType.Hand;

    public virtual void Init(PlayerUIState uiState, PlayerRaycast playerInteractor)
    {
        ui = uiState;
        interactor = playerInteractor;
    }

    // 도구가 선택될 때
    public virtual void OnSelect()
    {
        enabled = true;
    }

    // 도구가 선택 해제될 때
    public virtual void OnDeselect()
    {
        enabled = false;
    }

    public virtual void Tick() { }

    public virtual void FixedTick() { }
}
