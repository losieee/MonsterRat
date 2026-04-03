using UnityEngine;
using UnityEngine.UI;

public class TutorialStep : MonoBehaviour
{
    public TutorialDialogueSystem dialogue;
    public PlayerUIState playerUIState;
    public TutorialInventory inventory;

    public GameObject storeOutLine;
    public GameObject btnOutLine;
    public BoxCollider storeRange;
    public Button mopBtn;
    public Button gunBtn;
    public Button spannerBtn;
    public GameObject exitRange;

    private int _pausedIndex = -999;

    private void OnEnable()
    {
        dialogue.OnLineShown += HandleLineShown;
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
        dialogue.OnLineShown -= HandleLineShown;
        dialogue.OnPausedAtIndex -= HandlePause;

        if (playerUIState != null)
        {
            playerUIState.OnStoreOpened -= HandleStoreOpened;
            playerUIState.OnStoreClosed -= HandleStoreClosed;
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
        else if(idx == 12)
        {
            if(btnOutLine != null)
                btnOutLine.SetActive(true);
        }
    }

    void HandleLineShown(int idx)
    {
        if (idx == 23)
        {
            if (exitRange != null) 
                exitRange.SetActive(true);
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
        if (_pausedIndex != 9) return;

        _pausedIndex = -999;
        dialogue.ResumeAuto();
    }

    void HandleToolAdded(TutorialToolType tool)
    {
        if (_pausedIndex != 10) return;

        if (inventory != null && inventory.HasAllTutorialTools())
        {
            _pausedIndex = -999;
            dialogue.ResumeAuto();
        }
    }
}
