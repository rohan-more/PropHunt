using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [Header("Weapon Stats")]
    public int damage;
    public float range;
    public float fireRate;

    protected float lastFireTime;

    public virtual bool CanFire()
    {
        return Time.time >= lastFireTime + (1f / fireRate);
    }

    public void TryFire(Camera cam)
    {
        if (!CanFire())
        {
            return;
        }
        lastFireTime = Time.time;
        Fire(cam);
    }

    protected abstract void Fire(Camera cam);

    public virtual void OnEquip() { }
    public virtual void OnUnequip() { }
}