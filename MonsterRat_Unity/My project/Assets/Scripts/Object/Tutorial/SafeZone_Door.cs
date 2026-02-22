using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SafeZone_Door : MonoBehaviour
{
    Animator anim;

    public bool canWork = false;

    bool canOpen = true;    

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void OpenDoor()
    {
        if (!canOpen || !canWork) return;

        canOpen = false;
        anim.SetTrigger("DoorOpen");

        StartCoroutine(WaitCool(3f));
    }

    IEnumerator WaitCool(float val)
    {
        yield return new WaitForSeconds(val);
        canOpen = true;
    }

    public void OpenClearDoor()
    {
        anim.SetTrigger("ClearDoorOpen");
        StartCoroutine(ClearTutorial());
    }

    IEnumerator ClearTutorial()
    {
        yield return new WaitForSeconds(2);
        TutorialScreenFader.Instance.FadeOut(0.5f);
        yield return new WaitForSeconds(0.5f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("LobbyScene");
    }
}
