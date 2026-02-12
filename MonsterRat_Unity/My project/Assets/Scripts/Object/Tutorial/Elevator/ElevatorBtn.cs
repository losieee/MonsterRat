using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ElevatorBtn : MonoBehaviour
{
    Animator anim;

    bool canCloseDoor = true;
    public string sceneName;

    void Start()
    {
        anim = GetComponentInParent<Animator>();
    }


    public void ClickEvBtn()
    {
        if (!canCloseDoor) return;
        canCloseDoor = false;

        anim.SetTrigger("StartEv");
        StartCoroutine(CloseDoor(8f));
    }

    IEnumerator CloseDoor(float delay)
    {
        yield return new WaitForSeconds(delay);
        canCloseDoor = true;

        ScreenFader.Instance.FadeOut(0.5f);
        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(sceneName);
    }
}
