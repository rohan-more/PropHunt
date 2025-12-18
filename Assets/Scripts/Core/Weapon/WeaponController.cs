using System;
using Photon.Pun;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private Camera fpsCam;
    [SerializeField] private Animator animator;
    [SerializeField] private PhotonView photonView;

    [SerializeField] private Weapon activeWeapon;
    
    private void Update()
    {
        if (!photonView.IsMine) return;
        if (activeWeapon == null) return;

        if (Input.GetButton("Fire1"))
        {
            animator.SetBool("IsShooting", true);
            activeWeapon.TryFire(fpsCam);
        }
        else
        {
            animator.SetBool("IsShooting", false);
        }
    }

    public void EquipWeapon(Weapon newWeapon)
    {
        if (activeWeapon == newWeapon) return;

        if (activeWeapon != null)
            activeWeapon.OnUnequip();

        activeWeapon = newWeapon;
        activeWeapon.OnEquip();
    }
}