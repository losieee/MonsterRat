using UnityEngine;
using Fusion; // PhotonView 대신 Fusion 사용
using Photon.Voice.Unity;

// [RequireComponent] 속성은 PUN2의 PhotonView를 요구하므로 지워줍니다!
public class VoiceController : NetworkBehaviour
{
    private Recorder recorder;

    // Start() 대신 네트워크 셋업이 끝난 후 호출되는 Spawned()를 사용합니다.
    public override void Spawned()
    {
        // 내가 조종하는 캐릭터가 아니면 스크립트 끄기
        if (!HasInputAuthority)
        {
            // if (VoiceOn != null) VoiceOn.SetActive(false);
            // if (VoiceOff != null) VoiceOff.SetActive(false);

            enabled = false;
            return;
        }

        // Fusion에서는 Recorder가 보통 씬 매니저(VoiceManager 등) 쪽에 하나만 있습니다.
        // GetComponent 대신 씬에 존재하는 단 하나의 Recorder를 찾아옵니다.
        recorder = FindObjectOfType<Recorder>();

        if (recorder != null)
        {
            // 처음엔 마이크 꺼두기 (V키를 눌러야만 전송됨)
            recorder.TransmitEnabled = false;
        }
        else
        {
            Debug.LogWarning("씬에서 Recorder를 찾을 수 없습니다! Voice Client가 있는 곳에 Recorder가 잘 붙어있는지 확인해주세요.");
        }
    }

    void Update()
    {
        // 내 캐릭터가 아니거나 마이크를 못 찾았으면 리턴
        if (!HasInputAuthority || recorder == null) return;

        // V키를 누를 때 마이크 켜기
        if (Input.GetKeyDown(KeyCode.V))
        {
            recorder.TransmitEnabled = true;
            // if (VoiceOff != null) VoiceOff.SetActive(false);
            // if (VoiceOn != null) VoiceOn.SetActive(true);
        }
        // V키를 뗄 때 마이크 끄기
        else if (Input.GetKeyUp(KeyCode.V))
        {
            recorder.TransmitEnabled = false;
            // if (VoiceOn != null) VoiceOn.SetActive(false);
            // if (VoiceOff != null) VoiceOff.SetActive(true);
        }
    }
}