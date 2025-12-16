using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

public class LocalPlayerStatePresenter : MonoBehaviourPunCallbacks
{
    [SerializeField] private HealthView healthView;
    [SerializeField] private WeaponShooter weaponShooter;
    [SerializeField] private FirstPersonController controller;

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!targetPlayer.IsLocal) return;

        if (changedProps.ContainsKey("Health"))
        {
            int health = (int)changedProps["Health"];
            healthView.SetHealth(health);

            /*if (health <= 0)
                HandleDeath();*/
        }

        if (changedProps.ContainsKey("Role"))
        {
            HandleRole((string)changedProps["Role"]);
        }
    }

    private void HandleDeath()
    {
        controller.enabled = false;
        weaponShooter.enabled = false;
        // future: switch to spectator
    }

    private void HandleRole(string role)
    {
        // future-proof
    }
}