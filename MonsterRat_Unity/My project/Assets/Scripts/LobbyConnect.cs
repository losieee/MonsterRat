using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class RoomsResponse { public RoomInfo[] rooms; }

[Serializable]
public class RoomInfo
{
    public string roomId;
    public long createdAt;
    public int playerCount;
}

[Serializable]
public class CreateRoomResponse
{
    public bool ok;
    public string roomId;
    public string wsUrl;
}

[Serializable]
public class JoinRoomRequest { public string roomId; }

[Serializable]
public class JoinRoomResponse
{
    public bool ok;
    public string roomId;
    public string wsUrl;
    public string error;
}

public class LobbyConnect : MonoBehaviour
{
    [Header("예: http://192.168.0.10:3000")]
    public string lobbyBaseUrl = "http://127.0.0.1:3000";

    public IEnumerator GetRooms(Action<RoomsResponse, string> done)
    {
        using var req = UnityWebRequest.Get($"{lobbyBaseUrl}/rooms");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            done?.Invoke(null, req.error);
            yield break;
        }

        var json = req.downloadHandler.text;
        var data = JsonUtility.FromJson<RoomsResponse>(json);
        done?.Invoke(data, null);
    }

    public IEnumerator CreateRoom(Action<CreateRoomResponse, string> done)
    {
        using var req = new UnityWebRequest($"{lobbyBaseUrl}/rooms/create", "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{}"));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            done?.Invoke(null, req.error);
            yield break;
        }

        var data = JsonUtility.FromJson<CreateRoomResponse>(req.downloadHandler.text);
        done?.Invoke(data, null);
    }

    public IEnumerator JoinRoom(string roomId, Action<JoinRoomResponse, string> done)
    {
        var body = JsonUtility.ToJson(new JoinRoomRequest { roomId = roomId });
        using var req = new UnityWebRequest($"{lobbyBaseUrl}/rooms/join", "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            // 404 같은 경우에도 body 있을 수 있음
            var txt = req.downloadHandler?.text;
            if (!string.IsNullOrEmpty(txt))
            {
                try
                {
                    var parsed = JsonUtility.FromJson<JoinRoomResponse>(txt);
                    done?.Invoke(parsed, null);
                }
                catch
                {
                    done?.Invoke(null, req.error);
                }
            }
            else done?.Invoke(null, req.error);

            yield break;
        }

        var data = JsonUtility.FromJson<JoinRoomResponse>(req.downloadHandler.text);
        done?.Invoke(data, null);
    }
}