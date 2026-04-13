using UnityEngine;
using Photon.Pun;

public class Bullet : MonoBehaviour
{
    private float damage;
    private GameObject shooterRoot;
    private Vector3 direction;
    public float speed = 50f;
    public float lifeTime = 3f;
    private bool hasHit = false;

    public void Initialize(float damageValue, GameObject shooter, Vector3 dir)
    {
        damage = damageValue;
        shooterRoot = shooter;
        direction = dir.normalized;
    }

    void Start()
    {
        if (direction == Vector3.zero)
        {
            //Debug.LogWarning("Bullet direction is zero, using default forward");
            direction = transform.forward;
        }
        if (speed <= 0) speed = 50f;   // 确保速度为正
        transform.rotation = Quaternion.LookRotation(direction);
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        if (shooterRoot != null && other.transform.root.gameObject == shooterRoot)
            return;
        if (other.CompareTag("Bullet")) return;
        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            Health targetHealth = other.GetComponent<Health>();
            if (targetHealth != null)
            {
                // 只有射击者的本地客户端才能造成伤害
                PhotonView shooterPV = shooterRoot.GetComponent<PhotonView>();
                if (shooterPV != null && shooterPV.IsMine)
                {
                    hasHit = true;
                    targetHealth.TakeDamage(damage, shooterRoot);
                }
                else
                {
                    // 非射击者客户端的子弹不造成伤害，直接销毁
                    Destroy(gameObject);
                    return;
                }
            }
        }
        Destroy(gameObject);
    }
}