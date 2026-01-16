using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.UI;

public class RoomItem : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI roomNameText;  
    public TextMeshProUGUI playerCountText; 

    private string _sessionName; 
    private LobbyManager _manager; 

    
    public void Setup(SessionInfo session, LobbyManager manager)
    {
        _sessionName = session.Name;
        _manager = manager;
        if (roomNameText != null)
            roomNameText.text = session.Name; 
        if (playerCountText != null)
            playerCountText.text = $"{session.PlayerCount} / {session.MaxPlayers}"; // 인원수
    }
    public void OnClick_Join()
    {
        _manager.JoinRoom(_sessionName);
    }
}