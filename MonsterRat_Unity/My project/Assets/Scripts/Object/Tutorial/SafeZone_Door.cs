using System.Collections;
using UnityEngine;

public class SafeZone_Door : MonoBehaviour
{
    Animator anim;

    bool canOpen = true;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void OpenDoor()
    {
        if (!canOpen) return;

        canOpen = false;
        anim.SetTrigger("DoorOpen");

        StartCoroutine(WaitCool(3f));
    }

    IEnumerator WaitCool(float val)
    {
        yield return new WaitForSeconds(val);
        canOpen = true;
    }
}
