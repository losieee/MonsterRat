using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashRangeArrive : MonoBehaviour
{
    public TutorialDialogueSystem dialogue;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (dialogue != null)
                dialogue.ResumeAuto();

            gameObject.SetActive(false);
        }
    }
}
