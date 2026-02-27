using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class InteractionTextTrigger : MonoBehaviour
{
    public bool useE_Pickup = false;

    private static Text screenTextComponent;
    private static GameObject screenTextObject;

    void Start()
    {
        if (screenTextComponent == null)
        {
            GameObject foundObj = GameObject.Find("TriggerTeXt");

            if (foundObj != null)
            {
                screenTextObject = foundObj;
                screenTextComponent = foundObj.GetComponent<Text>();
                foundObj.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                ShowText();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                HideText();
            }
        }
    }

    void ShowText()
    {
        if (screenTextComponent == null) return;

        string finalMessage = "";

        // ÁÙ¹Ù²Þ ´ë½Å ¶ç¾î¾²±â 3Ä­("   ")À¸·Î ±¸ºÐ
        if (useE_Pickup)
        {
            finalMessage += "[ E : ¾ÆÀÌÅÛ È¹µæÇÏ±â ]   ";
        }

        screenTextComponent.text = finalMessage;
        screenTextObject.SetActive(true);
    }

    void HideText()
    {
        if (screenTextObject != null)
        {
            screenTextObject.SetActive(false);
        }
    }
}