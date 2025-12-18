using Core;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

public class LocalPlayerStatePresenter : MonoBehaviourPunCallbacks
{
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private FirstPersonController controller;

    private PlayerType localPlayerType;

    private void Start()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("PlayerType", out object typeObj))
        {
            localPlayerType = (PlayerType)typeObj;
            HandlePlayerType(localPlayerType);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!targetPlayer.IsLocal)
            return;

        if (changedProps.ContainsKey("Health"))
        {
            int health = (int)changedProps["Health"];

            if (health <= 0)
                HandleDeath();
        }

        if (changedProps.ContainsKey("PlayerType"))
        {
            localPlayerType = (PlayerType)changedProps["PlayerType"];
            HandlePlayerType(localPlayerType);
        }
    }

    private void HandleDeath()
    {
        controller.enabled = false;
        weaponController.enabled = false;

        // later:
        // switch to spectator
        // disable collisions
    }

    private void HandlePlayerType(PlayerType type)
    {
        if (type == PlayerType.HUNTER)
        {
            // Hunters are always armed
            if (weaponController != null) weaponController.enabled = true;
        }
        else
        {
            // Props may have limited or no weapons
            if (weaponController != null) weaponController.enabled = false;
        }
    }
}