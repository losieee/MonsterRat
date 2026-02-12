using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DeleteObject : MonoBehaviour
{
    Animator anim;
    public HashSet<GameObject> deleteBoxes = new HashSet<GameObject>();

    bool isDeleting = false;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void CanDelete()
    {
        if (isDeleting) return;
        isDeleting = true;

        anim.SetTrigger("PushButton");

        StartCoroutine(CloseDoor(2f));
    }

    IEnumerator CloseDoor(float delay)
    {
        yield return new WaitForSeconds(delay);

        var copy = new List<GameObject>(deleteBoxes);

        
        foreach (var box in copy)
        {
            if (box == null) continue;
            Destroy(box.transform.root.gameObject);
        }

        deleteBoxes.Clear();

        yield return new WaitForSeconds(3);
        isDeleting = false;
    }
}
