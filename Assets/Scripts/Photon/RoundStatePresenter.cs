using System;
using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine;

public static class ScoreKeys
{
    public const string HunterKills = "HunterKills";
    public const string KillTarget  = "KillTarget"; // win condition
}

public static class RoundKeys
{
    public const string StartTime = "RoundStartTime";
    public const string Duration  = "RoundDuration";
    public const string State     = "RoundState";
}

public static class PlayerPropertyKeys
{
    public const string PlayerType = "PlayerType";
    public const string Health = "Health";
    public const string SurvivalScore = "SurvivalScore";
}

public class RoundStatePresenter : MonoBehaviourPunCallbacks
{
    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps.ContainsKey(ScoreKeys.HunterKills))
        {
            int kills = (int)changedProps[ScoreKeys.HunterKills];
            Debug.Log($"[UI] Hunter Kills: {kills}");
        }

        if (changedProps.ContainsKey(RoundKeys.State))
        {
            Debug.Log($"[Round] State = {changedProps[RoundKeys.State]}");
        }
    }

    private void Update()
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoundKeys.StartTime)) return;

        double start = (double)PhotonNetwork.CurrentRoom.CustomProperties[RoundKeys.StartTime];
        int duration = (int)PhotonNetwork.CurrentRoom.CustomProperties[RoundKeys.Duration];
        double elapsedTime = PhotonNetwork.Time - start;
        double remaining = Math.Max(0.0, duration - elapsedTime);

        if (remaining <= 0 && PhotonNetwork.IsMasterClient)
        {
            EndRound();
        }
    }

    private void EndRound()
    {
        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new Hashtable { { RoundKeys.State, "Ended" } }
        );
    }
}