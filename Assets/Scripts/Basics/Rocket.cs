using UnityEngine;
using Photon.Pun;

public class Rocket : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 5f;
    public float explosionRadius = 1f;          // 增大范围更合理
    public float explosionDamage = 30f;         // 可单独设置爆炸伤害
    public GameObject explosionEffect;

    private GameObject shooterRoot;
    private Vector3 direction;

    public void Initialize(GameObject shooter, Vector3 dir)
    {
        shooterRoot = shooter;
        direction = dir.normalized;
    }

    void Start()
    {
        if (direction == Vector3.zero)
        {
            //Debug.LogWarning("Rocket direction is zero, using default forward");
            direction = transform.forward;
        }
        if (speed <= 0) speed = 20f;
        transform.rotation = Quaternion.LookRotation(direction);
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        // 忽略开枪者自身
        if (shooterRoot != null && other.transform.root.gameObject == shooterRoot)
            return;

        // 爆炸
        Explode();

        // 销毁火箭弹
        Destroy(gameObject);
    }

    private void Explode()
    {
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // 只有射击者的本地客户端才造成伤害
        PhotonView shooterPV = shooterRoot.GetComponent<PhotonView>();
        if (shooterPV == null || !shooterPV.IsMine)
        {
            Destroy(gameObject);
            return;
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player") || col.CompareTag("Enemy"))
            {
                Health targetHealth = col.GetComponent<Health>();
                if (targetHealth != null)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    float damageMultiplier = 1f - (distance / explosionRadius);
                    float finalDamage = explosionDamage * Mathf.Max(0.1f, damageMultiplier);
                    targetHealth.TakeDamage(finalDamage, shooterRoot);
                }
            }
        }
    }
}