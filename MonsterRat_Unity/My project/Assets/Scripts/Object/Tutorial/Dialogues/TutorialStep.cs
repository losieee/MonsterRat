using UnityEngine;
using UnityEngine.UI;

public class TutorialStep : MonoBehaviour
{
    public TutorialDialogueSystem dialogue;
    public PlayerUIState playerUIState;
    public Inventory inventory;

    public GameObject storeOutLine;
    public GameObject btnOutLine;
    public BoxCollider storeRange;
    public Button mopBtn;
    public Button gunBtn;
    public Button spannerBtn;

    private int _pausedIndex = -999;

    private void OnEnable()
    {
        dialogue.OnPausedAtIndex += HandlePause;

        if (playerUIState != null)
        {
            playerUIState.OnStoreOpened += HandleStoreOpened;
            playerUIState.OnStoreClosed += HandleStoreClosed;
        }
        if (inventory != null)
            inventory.OnToolAdded += HandleToolAdded;
    }

    private void OnDisable()
    {
        dialogue.OnPausedAtIndex -= HandlePause;

        if (playerUIState != null)
        {
            playerUIState.OnStoreOpened += HandleStoreOpened;
            playerUIState.OnStoreClosed += HandleStoreClosed;
        }
        if (inventory != null)
            inventory.OnToolAdded -= HandleToolAdded;
    }

    void HandlePause(int idx)
    {
        _pausedIndex = idx;

        if (idx == 7)
        {
            if (storeOutLine != null)
            {
                storeOutLine.SetActive(true);
                storeRange.enabled = true;
            }
        }
        else if(idx == 9)
        {
            if(mopBtn != null && gunBtn != null && spannerBtn != null)
            {
                mopBtn.enabled = true;
                gunBtn.enabled = true;
                spannerBtn.enabled = true;
            }
        }
        else if(idx == 11)
        {
            if(btnOutLine != null)
                btnOutLine.SetActive(true);
        }
    }

    void HandleStoreOpened()
    {
        if (_pausedIndex != 7) return;

        if (storeOutLine != null)
            storeOutLine.SetActive(false);

        _pausedIndex = -999;

        dialogue.ResumeAuto();
    }

    void HandleStoreClosed()
    {
        if (_pausedIndex != 10) return;

        _pausedIndex = -999;
        dialogue.ResumeAuto();
    }

    void HandleToolAdded(ToolType tool)
    {
        if (_pausedIndex != 9) return;

        if (inventory != null && inventory.HasAllTutorialTools())
        {
            _pausedIndex = -999;
            dialogue.ResumeAuto();
        }
    }
}
