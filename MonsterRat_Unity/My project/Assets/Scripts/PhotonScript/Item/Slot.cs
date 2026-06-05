using UnityEngine;
using UnityEngine.UI;
// using System.Collections.Generic; // 필요 없음
// using Photon.Pun; // 필요 없음

public class Slot : MonoBehaviour
{
    public Image icon;
    public Sprite blankImage;



    public void DrawSlot(ItemData itemdata)
    {


        if (icon == null) return;
        if (itemdata == null) return;


        if (itemdata.itemIcon == null)
        {
            icon.sprite = null;
        }
        else
        {
            icon.sprite = itemdata.itemIcon;
        }

        icon.enabled = true;
    }

    public void ClearSlot()
    {

        if (icon == null) return;

        icon.sprite = blankImage;
    }
}