using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

public class DamageableTarget : MonoBehaviourPunCallbacks
{
    [SerializeField] private int maxHealth = 50;

    private string HealthKey => "DummyHealth_" + photonView.ViewID;

    private void Start()
    {
        Debug.Log($"[Dummy] Start | Name={name} ViewID={photonView.ViewID} IsMaster={PhotonNetwork.IsMasterClient}");

        // Master initializes health once
        if (PhotonNetwork.IsMasterClient)
        {
            if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(HealthKey))
            {
                PhotonNetwork.CurrentRoom.SetCustomProperties(
                    new Hashtable { { HealthKey, maxHealth } }
                );

                Debug.Log($"[Dummy] Initialized Health={maxHealth}");
            }
        }

        LogCurrentHealth("Start()");
    }

    private void HandleDeath()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int kills = (int)PhotonNetwork.CurrentRoom.CustomProperties[ScoreKeys.HunterKills];
        kills++;

        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new ExitGames.Client.Photon.Hashtable
            {
                { ScoreKeys.HunterKills, kills }
            }
        );

        Debug.Log($"[Score] HunterKills = {kills}");
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (!changedProps.ContainsKey(HealthKey)) return;

        int health = (int)changedProps[HealthKey];
        Debug.Log($"[Dummy] OnRoomPropertiesUpdate | Name={name} ViewID={photonView.ViewID} Health={health}");
    }

    private void LogCurrentHealth(string context)
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            Debug.Log($"[Dummy] {context} | No room yet");
            return;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(HealthKey, out object value))
        {
            Debug.Log($"[Dummy] {context} | HealthKey not found");
            return;
        }

        Debug.Log($"[Dummy] {context} | Name={name} ViewID={photonView.ViewID} Health={(int)value}");
    }

    [PunRPC]
    public void RPC_RequestDamage(int damage)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int current = (int)PhotonNetwork.CurrentRoom.CustomProperties[HealthKey];
        int next = Mathf.Max(0, current - damage);

        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new Hashtable { { HealthKey, next } }
        );

        Debug.Log($"[Dummy] Damage Applied | Damage={damage} NewHealth={next}");

        if (next == 0)
        {
            HandleDeath();
        }
    }
}