using UnityEngine;
using TMPro;
using Photon.Realtime; // RoomInfo를 사용하기 위해 필요합니다.
using Photon.Pun;

public class RoomItem : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI playerCountText;

    private string _roomName;
    private LobbyManager _lobbyManager;

    // 1. 매개변수 타입을 SessionInfo에서 RoomInfo로 변경합니다.
    public void Setup(RoomInfo info, LobbyManager lobbyManager)
    {
        _lobbyManager = lobbyManager;
        _roomName = info.Name;

        // 2. PUN2의 RoomInfo 속성에 맞게 텍스트 설정
        roomNameText.text = info.Name;
        playerCountText.text = $"{info.PlayerCount} / {info.MaxPlayers}";
    }

    // 버튼 눌렀을 때 실행될 함수 (Inspector에서 Button의 OnClick에 연결)
    public void OnClick_Join()
    {
        _lobbyManager.JoinRoom(_roomName);
    }
}