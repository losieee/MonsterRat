using UnityEngine;
using UnityEngine.UI;

public class RoomItemUI : MonoBehaviour
{
    public Text label;
    private string roomId;
    private LobbyManager lobby;

    public void Setup(LobbyManager lobbyManager, string id, int count, bool started)
    {
        lobby = lobbyManager;
        roomId = id;

        if (label == null)
            label = GetComponentInChildren<Text>();

        string startedTag = started ? " [STARTED]" : "";
        label.text = $"{roomId} ({count}/2){startedTag}";

        // 시작된 방은 클릭 막고 싶으면
        var btn = GetComponent<Button>();
        if (btn != null)
            btn.interactable = !started;
    }

    public void OnClickJoin()
    {
        if (lobby != null && !string.IsNullOrEmpty(roomId))
            lobby.JoinRoomById(roomId);
    }
}
