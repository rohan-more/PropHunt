using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class HitscanWeapon : Weapon
{
    protected override void Fire(Camera cam)
    {
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, range))
        {
            TryHandleHit(hit);
        }
    }

    private void TryHandleHit(RaycastHit hit)
    {
        // Case 1: Player
        if (hit.transform.CompareTag("TP_Player"))
        {
            PhotonView playerView = hit.transform.GetComponent<PhotonView>();
            if (playerView == null) return;

            RequestPlayerDamage(playerView.OwnerActorNr);
            return;
        }

        // Case 2: Damageable dummy / object
        if (hit.transform.TryGetComponent(out DamageableTarget damageable))
        {
            RequestObjectDamage(damageable);
            return;
        }
    }
    
    private void RequestPlayerDamage(int targetActor)
    {
        PhotonView shooterView = GetComponentInParent<PhotonView>();

        shooterView.RPC(
            nameof(RPC_RequestDamage),
            RpcTarget.MasterClient,
            targetActor,
            damage
        );
    }
    
    private void RequestObjectDamage(DamageableTarget target)
    {
        target.photonView.RPC(
            nameof(DamageableTarget.RPC_RequestDamage),
            RpcTarget.MasterClient,
            damage
        );
    }

    private void RequestDamage(int targetActor)
    {
        PhotonView pv = GetComponentInParent<PhotonView>();
        pv.RPC(nameof(RPC_RequestDamage), RpcTarget.MasterClient, targetActor, damage);
    }

    [PunRPC]
    private void RPC_RequestDamage(int targetActor, int dmg, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player target = PhotonNetwork.CurrentRoom.GetPlayer(targetActor);
        if (target == null) return;

        int currentHealth = (int)target.CustomProperties["Health"];
        int newHealth = Mathf.Max(0, currentHealth - dmg);

        ExitGames.Client.Photon.Hashtable props = new();
        props["Health"] = newHealth;
        target.SetCustomProperties(props);
    }
}