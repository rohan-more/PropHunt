using System.Collections.Generic;
using Core.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Random = UnityEngine.Random;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    private string _errorMessage;
    private string _roomNameText;
    private string _lastRoomCreateAttempt;

    public string ErrorMessage
    {
        get => _errorMessage;
        set => _errorMessage = value;
    }

    void Start()
    {
        Debug.Log("[PhotonManager] Start() - Connecting using settings...");
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log($"[PhotonManager] PhotonNetwork.NetworkClientState: {PhotonNetwork.NetworkClientState}");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log($"[PhotonManager] OnConnectedToMaster - AppId: {PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime} Region: {PhotonNetwork.CloudRegion} GameVersion: {PhotonNetwork.GameVersion}");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
        Debug.Log("[PhotonManager] JoinLobby() called and AutomaticallySyncScene enabled.");
    }

    public override void OnJoinedLobby()
    {
        Events.OnShowTab(TabName.LOBBY);
        int number = Random.Range(0, 100);
        PhotonNetwork.NickName = "Player " + number;
        Debug.Log($"[PhotonManager] OnJoinedLobby - Joined lobby. Assigned NickName: {PhotonNetwork.NickName}");
    }

    public void CreateRoom(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("[PhotonManager] CreateRoom called with empty roomName. Aborting.");
            ErrorMessage = "Room name cannot be empty.";
            Events.OnCreateRoomFailure(roomName);
            return;
        }

        _lastRoomCreateAttempt = roomName;
        Debug.Log($"[PhotonManager] CreateRoom - Attempting to create room '{roomName}'");
        var options = new RoomOptions { MaxPlayers = 8, IsVisible = true, IsOpen = true };
        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
        Events.OnShowTab(TabName.LOADING);
    }

    public override void OnJoinedRoom()
    {
        string roomName = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "(null)";
        Debug.Log($"[PhotonManager] OnJoinedRoom - Joined room: {roomName} | Players: {PhotonNetwork.CurrentRoom?.PlayerCount}");
        Events.OnRoomName(roomName);
        Events.OnShowTab(TabName.ROOM);
        Events.OnUpdatePlayerList();
        // OnMasterLeftRoom seems like a poorly named event to call here, but preserving original behavior
        //Events.OnMasterLeftRoom();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ErrorMessage = message;
        Debug.LogError($"[PhotonManager] OnCreateRoomFailed - AttemptedRoom: '{_lastRoomCreateAttempt}' ReturnCode: {returnCode} Message: {message}");
        Events.OnCreateRoomFailure(_lastRoomCreateAttempt);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[PhotonManager] OnJoinRoomFailed - ReturnCode: {returnCode} Message: {message}");
        ErrorMessage = message;
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[PhotonManager] OnLeftRoom - Left room, showing lobby tab.");
        Events.OnShowTab(TabName.LOBBY);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[PhotonManager] OnMasterClientSwitched - New host: {newMasterClient?.NickName} (ID:{newMasterClient?.ActorNumber})");
        Events.OnMasterLeftRoom();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"[PhotonManager] OnRoomListUpdate - Received {roomList?.Count ?? 0} rooms.");
        Events.OnUpdateRoomList(roomList);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        string roomName = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "(null)";
        Debug.Log($"[PhotonManager] OnPlayerEnteredRoom - {newPlayer.NickName} has entered room '{roomName}'. ActorNumber: {newPlayer.ActorNumber} UserId: {newPlayer.UserId}");
        Events.OnPlayerEnteredRoom(newPlayer);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"[PhotonManager] OnDisconnected - Reason: {cause}");
        ErrorMessage = $"Disconnected: {cause}";
    }
}
