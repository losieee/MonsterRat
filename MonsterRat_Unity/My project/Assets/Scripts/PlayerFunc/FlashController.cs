using UnityEngine;

public class FlashController : InvenBase
{
    public override ToolType Type => ToolType.Flash;

    public GameObject flash;

    bool flashOn;

    private void OnDisable()
    {
        FlashOff();
    }

    public override void Tick()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (flashOn) FlashOff();
            else FlashOn();
        }
    }

    void FlashOn()
    {
        flashOn = true;

        if (flash != null)
            flash.SetActive(true);
    }

    void FlashOff()
    {
        flashOn = false;

        if (flash != null)
            flash.SetActive(false);
    }
}
