using Photon.Pun;
using UnityEngine;

public class HitscanWeapon : Weapon
{
    [SerializeField] private PlayerNetworkAuthority playerAuthority;

    protected override void Fire(Camera cam)
    {
        if (Physics.Raycast(
                cam.transform.position,
                cam.transform.forward,
                out RaycastHit hit,
                range))
        {
            TryHandleHit(hit);
        }
    }

    private void TryHandleHit(RaycastHit hit)
    {
        PhotonView targetView =
            hit.transform.GetComponentInParent<PhotonView>();

        if (targetView == null)
            return;

        if (!targetView.TryGetComponent<IDamageable>(out _))
            return;

        playerAuthority.RequestDamage(targetView.ViewID, damage);
    }
}