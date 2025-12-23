using Core;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

public class PlayerHUDPresenter : MonoBehaviourPunCallbacks
{
    [Header("HUD Roots")]
    [SerializeField] private GameObject hunterHUDRoot;
    [SerializeField] private GameObject propHUDRoot;

    [Header("Views")]
    [SerializeField] private HunterHUDView hunterHUD;
    [SerializeField] private PropHUDView propHUD;

    private PlayerType localRole;

    private void Start()
    {
        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("PlayerType", out object roleObj))
        {
            Debug.LogWarning("[HUD] Role not set yet");
            return;
        }

        localRole = (PlayerType)System.Enum.Parse(typeof(PlayerType), roleObj.ToString());
        ConfigureForRole(localRole);
        if (localRole == PlayerType.HUNTER)
        {
            hunterHUD.SetPlayerName(PhotonNetwork.NickName);
        }
        else
        {
            propHUD.SetPlayerName(PhotonNetwork.NickName);
        }
    }

    private void ConfigureForRole(PlayerType role)
    {
        hunterHUDRoot.SetActive(role == PlayerType.HUNTER);
        propHUDRoot.SetActive(role == PlayerType.PROP);

        if (role == PlayerType.PROP)
        {
            propHUD.InitializeHealth(10); // temp, replace with actual max health
        }
    }

    /* -------- Room state -------- */

    private void Update()
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        if (!props.ContainsKey("RoundStartTime")) return;

        double start = (double)props["RoundStartTime"];
        int duration = (int)props["RoundDuration"];

        double remaining =
            System.Math.Max(0.0, duration - (PhotonNetwork.Time - start));

        if (localRole == PlayerType.HUNTER)
            hunterHUD.SetTimer(remaining);
        else
            propHUD.SetTimer(remaining);
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (localRole == PlayerType.HUNTER && changedProps.ContainsKey("HunterKills"))
        {
            hunterHUD.SetKillCount((int)changedProps["HunterKills"]);
        }
    }

    /* -------- Player state -------- */

    public override void OnPlayerPropertiesUpdate(Player target, Hashtable changedProps)
    {
        if (!target.IsLocal) return;

        if (localRole == PlayerType.PROP && changedProps.ContainsKey("Health"))
        {
            Debug.Log("OnPlayerPropertiesUpdate Health: " + changedProps["Health"]);
            propHUD.SetHealth((int)changedProps["Health"]);
        }

        if (localRole == PlayerType.PROP && changedProps.ContainsKey("SurvivalScore"))
        {
            propHUD.SetSurvivalScore((int)changedProps["SurvivalScore"]);
        }
    }
}
