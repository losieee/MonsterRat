using UnityEngine;
using TMPro;
using Fusion;  

public class RoomItem : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI playerCountText;

    private string _roomName;
    private LobbyManager _lobbyManager;

    public void Setup(SessionInfo info, LobbyManager lobbyManager)
    {
        _lobbyManager = lobbyManager;
        _roomName = info.Name;  

        roomNameText.text = info.Name;
        playerCountText.text = $"{info.PlayerCount} / {info.MaxPlayers}";
    }

    public void OnClick_Join()
    {
        _lobbyManager.JoinRoom(_roomName);
    }
}