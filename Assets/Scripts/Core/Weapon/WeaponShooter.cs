using System.Collections;
using System.Collections.Generic;
using System.IO;
using Core;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class WeaponShooter : MonoBehaviour
{
    [SerializeField] private float rayMaxDistance;
    [SerializeField] private Camera fpsCam;
    [SerializeField] private PhotonView _photonView;
    [SerializeField] private Animator _animator;
    private PhotonView targetView;
    private void ShootRay()
    {

        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out RaycastHit hit, rayMaxDistance))
        {
            _animator.SetBool("IsShooting", true);
            if (hit.transform.CompareTag("TP_Player"))
            {
                PhotonView targetView = hit.transform.GetComponent<PhotonView>();
                if (targetView != null)
                {
                    ReportHit(targetView, 1);
                }
            }
            
            if (hit.transform.CompareTag("Prop_Clone"))
            {
                int viewID = hit.transform.GetComponent<PhotonView>().ViewID;
                PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Explosion"), hit.transform.position, Quaternion.identity);
                _photonView.RPC("RPC_DestroyProp", RpcTarget.OthersBuffered, viewID);
                OnDecoyHit(hit.transform.GetComponent<PhotonView>());
            }
            
        }
    }
    
    void ReportHit(PhotonView targetView, int damage)
    {
        PhotonNetwork.RaiseEvent(GameplayEvents.PropHit,
            new object[]
            {
                PhotonNetwork.LocalPlayer.ActorNumber, // hunter
                targetView.ViewID,
                damage
            },
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
    }
    
    void OnDecoyHit(PhotonView decoyView)
    {
        PhotonNetwork.RaiseEvent(GameplayEvents.DecoyDestroyed,
            new object[]
            {
                PhotonNetwork.LocalPlayer.ActorNumber,   // hunter
                decoyView.Owner.ActorNumber               // prop owner
            },
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
    }
    
    void OnPropKilled(PhotonView propView)
    {
        PhotonNetwork.RaiseEvent(GameplayEvents.PropKilled,
            new object[]
            {
                PhotonNetwork.LocalPlayer.ActorNumber, // hunter
                propView.Owner.ActorNumber              // prop
            },
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
    }
    
    void Update() 
    {
        if (Input.GetButton("Fire1"))
        {
            ShootRay();
        }
        if (Input.GetButtonUp("Fire1"))
        {
            _animator.SetBool("IsShooting", false);
        }
    }
}
