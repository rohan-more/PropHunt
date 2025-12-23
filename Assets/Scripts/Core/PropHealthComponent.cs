using Photon.Pun;
using UnityEngine;


public interface IDamageable
{
    void ApplyDamage(int damage);
}
public class PropHealthComponent : MonoBehaviourPun, IDamageable
{
    public int maxHealth = 10;
    private int currentHealth;
    public bool IsDead => currentHealth <= 0;
    
        
    private PropHUDView hud;
    void Awake()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            currentHealth = maxHealth;
        }

    }

    public void ApplyDamage(int damage)
    {
        /*
        if (!PhotonNetwork.IsMasterClient)
            return;
            */

        currentHealth = Mathf.Max(0, currentHealth - damage);

        photonView.RPC(nameof(RPC_SyncHealth), RpcTarget.All, currentHealth);

        if (currentHealth <= 0)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    private void RPC_SyncHealth(int health)
    {
        currentHealth = health;
        if (photonView.IsMine && hud == null)
        {
            hud = FindObjectOfType<PropHUDView>();
        }
        if(hud != null)
        {
            hud.SetHealth(health);
        }

        // update UI here
    }
    
}