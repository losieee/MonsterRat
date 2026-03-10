using UnityEngine;
using UnityEngine.UI;
using Fusion; // Photon.Pun 대신 Fusion을 사용합니다!

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
            // PhotonView 대신 Fusion의 NetworkObject를 가져옵니다.
            NetworkObject netObj = other.GetComponent<NetworkObject>();

            // pv.IsMine 역할을 하는 HasInputAuthority로 내 캐릭터인지 확인합니다.
            if (netObj != null && netObj.HasInputAuthority)
            {
                ShowText();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponent<NetworkObject>();

            if (netObj != null && netObj.HasInputAuthority)
            {
                HideText();
            }
        }
    }

    void ShowText()
    {
        if (screenTextComponent == null) return;

        string finalMessage = "";

        // 줄바꿈 대신 띄어쓰기 3칸("   ")으로 구분
        if (useE_Pickup)
        {
            finalMessage += "[ E : 아이템 획득하기 ]   ";
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