using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;

public class Health : MonoBehaviourPun
{
    [Header("引用")]
    [SerializeField] private PlayerStats stats;

    [Header("事件")]
    public UnityEvent OnDamageTaken;
    public UnityEvent OnDeath;
    public UnityEvent<float> OnHealthChanged;

    private bool isDead = false;
    private int lastAttackerViewID = -1;

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<PlayerStats>();
    }

    private void Start()
    {
        if (stats != null)
            stats.OnHealthChanged.AddListener(HandleHealthChanged);
    }

    private void HandleHealthChanged(float newHealth)
    {
        OnHealthChanged?.Invoke(newHealth);
        if (newHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    public void TakeDamage(float rawDamage, GameObject damageSource = null, DamageType type = DamageType.Physical, bool isCritOverride = false)
    {
        if (isDead || stats == null) return;

        if (damageSource != null)
        {
            PhotonView pv = damageSource.GetComponent<PhotonView>();
            if (pv != null) lastAttackerViewID = pv.ViewID;
        }

        float finalDamage = CalculateDamage(rawDamage, type, out bool isCrit);
        if (isCritOverride) isCrit = true;

        // 联机：由攻击者客户端发送 RPC（如果攻击者有 PhotonView 且属于本地玩家）
        if (PhotonNetwork.InRoom)
        {
            PhotonView attackerPV = damageSource != null ? damageSource.GetComponent<PhotonView>() : null;
            if (attackerPV != null && attackerPV.IsMine)
            {
                int targetViewID = photonView.ViewID;
                attackerPV.RPC("RPC_DealDamage", RpcTarget.All, targetViewID, finalDamage, isCrit, lastAttackerViewID);
            }
            else if (!PhotonNetwork.IsConnected)
            {
                // 离线模式
                ApplyDamage(finalDamage, isCrit);
            }
        }
        else
        {
            ApplyDamage(finalDamage, isCrit);
        }

        if (damageSource != null)
        {
            PhotonView pv = damageSource.GetComponentInParent<PhotonView>();
            if (pv != null)
            {
                lastAttackerViewID = pv.ViewID;
                //Debug.Log($"[{photonView.Owner}] 受击，攻击者 ViewID={lastAttackerViewID}");
            }
        }
    }

    [PunRPC]
    private void RPC_DealDamage(int targetViewID, float damage, bool isCrit, int attackerViewID)
    {
        PhotonView targetPV = PhotonView.Find(targetViewID);
        if (targetPV != null)
        {
            Health targetHealth = targetPV.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.lastAttackerViewID = attackerViewID; // ✅ 远程客户端记录攻击者
                targetHealth.ApplyDamage(damage, isCrit);
            }
        }
    }

    private float CalculateDamage(float rawDamage, DamageType type, out bool isCrit)
    {
        isCrit = false;
        if (stats == null) return rawDamage;

        float armorReduction = stats.CurrentArmor;
        float afterArmor = rawDamage * (1f - armorReduction);

        float resistance = 0f;
        switch (type)
        {
            case DamageType.Fire: resistance = stats.DefenseFire; break;
            case DamageType.Electric: resistance = stats.DefenseElectric; break;
            case DamageType.Poison: resistance = stats.DefensePoison; break;
        }
        afterArmor *= (1f - resistance * 0.5f);

        if (Random.value < stats.CurrentCritChance)
        {
            afterArmor *= stats.CritDamageMultiplier;
            isCrit = true;
        }

        return Mathf.Max(1f, afterArmor);
    }

    public void ApplyDamage(float damage, bool isCrit)
    {
        if (isDead) return;

        stats.ModifyHealth(-damage);
        OnDamageTaken?.Invoke();
        DamagePopupManager.Instance?.ShowPopup(transform.position, damage, isCrit);
    }

    [PunRPC]
    private void RPC_TakeDamage(float damage, int sourceViewID, bool isCrit)
    {
        lastAttackerViewID = sourceViewID;
        ApplyDamage(damage, isCrit);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        //Debug.Log($"[{photonView.Owner}] 死亡，lastAttackerViewID={lastAttackerViewID}");
        OnDeath?.Invoke();

        // 只有死亡者自己是本地玩家时，才通知攻击者增加击杀数
        // （因为死亡事件会在所有客户端触发，但我们只想让攻击者的客户端处理一次）
        if (photonView.IsMine)
        {
            if (lastAttackerViewID != -1)
            {
                PhotonView attackerView = PhotonView.Find(lastAttackerViewID);
                if (attackerView != null && attackerView.Owner != photonView.Owner) // 排除自杀
                {
                    // 调用攻击者客户端上的 RPC 增加击杀数
                    attackerView.RPC("RPC_AddKill", attackerView.Owner);
                }
            }
        }
    }

    public void Heal(float amount, bool isPercent = false)
    {
        if (isDead || stats == null) return;
        float healAmount = isPercent ? stats.MaxHealth * (amount / 100f) : amount;

        if (PhotonNetwork.InRoom && photonView.IsMine)
        {
            // 本地玩家发起治疗，通过 RPC 同步到所有客户端
            photonView.RPC("RPC_Heal", RpcTarget.All, healAmount);
        }
        else if (!PhotonNetwork.InRoom)
        {
            ApplyHeal(healAmount);
        }
    }

    [PunRPC]
    private void RPC_Heal(float healAmount)
    {
        ApplyHeal(healAmount);
    }

    private void ApplyHeal(float healAmount)
    {
        if (isDead) return;
        stats.ModifyHealth(healAmount);
        // 显示治疗跳字（绿色）
        DamagePopupManager.Instance?.ShowPopup(transform.position, healAmount, false, true);
    }

    public void ResetHealth()
    {
        isDead = false;
        stats?.ResetStats();
    }

    public bool IsDead => isDead;
}

public enum DamageType
{
    Physical,
    Fire,
    Electric,
    Poison
}