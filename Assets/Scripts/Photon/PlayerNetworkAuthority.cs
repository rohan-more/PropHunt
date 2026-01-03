using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerNetworkAuthority : MonoBehaviourPun
{
    public void RequestDamage(int targetViewId, int damage)
    {
        if (!photonView.IsMine)
            return;

        photonView.RPC(
            nameof(RPC_RequestDamage),
            RpcTarget.MasterClient,
            targetViewId,
            damage
        );
    }

    // ===== RPCs (Master authoritative) =====

    [PunRPC]
    private void RPC_RequestDamage(
        int targetViewId,
        int damage,
        PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        PhotonView targetView = PhotonView.Find(targetViewId);
        if (targetView == null)
            return;

        if (!targetView.TryGetComponent<IDamageable>(out var damageable))
            return;

        damageable.ApplyDamage(damage);
    }
}