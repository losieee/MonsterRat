using UnityEngine;
using Photon.Pun;
using Photon.Voice.Unity;
using UnityEngine.UI;

[RequireComponent(typeof(PhotonView), typeof(Recorder))]
public class VoiceController : MonoBehaviour
{
    private Recorder recorder;
    private PhotonView photonView;

   // public GameObject VoiceOn;
   // public GameObject VoiceOff;

    void Start()
    {
        photonView = GetComponent<PhotonView>();
        recorder = GetComponent<Recorder>();

        if (!photonView.IsMine)
        {
          //  if (VoiceOn != null) VoiceOn.SetActive(false);
          //  if (VoiceOff != null) VoiceOff.SetActive(false);

            enabled = false;
            return;
        }

       // if (VoiceOn != null) VoiceOn.SetActive(false);
       // if (VoiceOff != null) VoiceOff.SetActive(true);

        if (recorder != null)
        {
            recorder.TransmitEnabled = false;
        }
    }

    void Update()
    {
        if (recorder == null) return;

        if (Input.GetKeyDown(KeyCode.V))
        {
            recorder.TransmitEnabled = true;
           // if (VoiceOff != null) VoiceOff.SetActive(false);
           // if (VoiceOn != null) VoiceOn.SetActive(true);
        }
        else if (Input.GetKeyUp(KeyCode.V))
        {
            recorder.TransmitEnabled = false;
            //if (VoiceOn != null) VoiceOn.SetActive(false);
            //if (VoiceOff != null) VoiceOff.SetActive(true);
        }
    }
}