using Photon.Pun;
using UnityEngine;

public class DecoyHealthComponent : MonoBehaviourPun, IDamageable
{
    private const int MAX_HEALTH = 1;
    private int currentHealth;

    void Awake()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            currentHealth = MAX_HEALTH;
        }
    }

    public void ApplyDamage(int damage)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        // Decoys die on first hit
        PhotonNetwork.Destroy(gameObject);
    }
}