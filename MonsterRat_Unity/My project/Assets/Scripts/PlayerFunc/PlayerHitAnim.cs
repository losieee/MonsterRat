using System.Collections;
using UnityEngine;

public class PlayerHitAnim : MonoBehaviour
{
    private Animator anim;
    private float animSec = 0.5f;
    bool isHit = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void PlayerHit()
    {
        if (isHit) return;

        StartCoroutine(PlayAnim());
    }

    IEnumerator PlayAnim()
    {
        isHit = true;
        anim.SetTrigger("PlayerHit");
        yield return new WaitForSeconds(animSec);
        isHit = false;
    }
}
