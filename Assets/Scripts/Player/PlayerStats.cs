using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;

public class PlayerStats : MonoBehaviourPun, IPunObservable   // ✅ 添加接口
{
    [Header("基础属性")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float baseSpeed = 1.2f;
    [SerializeField] private float baseDamage = 10f;

    [Header("防御属性")]
    [Range(0f, 1f)]
    [SerializeField] private float armorPercent = 0f;
    [SerializeField] private float defenseElectric = 0f;
    [SerializeField] private float defenseFire = 0f;
    [SerializeField] private float defensePoison = 0f;

    [Header("暴击属性")]
    [Range(0f, 1f)]
    [SerializeField] private float critChance = 0.05f;
    [SerializeField] private float critDamageMultiplier = 2f;

    private float currentHealth;
    public float CurrentHealth => currentHealth;

    [System.Serializable]
    public class StatEvent : UnityEvent<float> { }
    public StatEvent OnHealthChanged;
    public UnityEvent OnSpeedChanged;

    private float speedBonus = 0f;
    private float damageBonus = 0f;
    private float armorBonus = 0f;
    private float critChanceBonus = 0f;

    public float MaxHealth => maxHealth;
    public float CurrentSpeed => baseSpeed + speedBonus;
    public float CurrentDamage => baseDamage + damageBonus;
    public float CurrentArmor => Mathf.Clamp01(armorPercent + armorBonus);
    public float CurrentCritChance => Mathf.Clamp01(critChance + critChanceBonus);
    public float CritDamageMultiplier => critDamageMultiplier;

    public float DefenseElectric => defenseElectric;
    public float DefenseFire => defenseFire;
    public float DefensePoison => defensePoison;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            PlayerMain player = GetComponent<PlayerMain>();
            if (player != null) player.speed = CurrentSpeed;
        }
    }

    public void SetHealth(float value)
    {
        float oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        if (oldHealth != currentHealth)
            OnHealthChanged?.Invoke(currentHealth);
    }

    public void ModifyHealth(float delta) => SetHealth(currentHealth + delta);

    public void AddSpeedBonus(float bonus) { speedBonus += bonus; UpdatePlayerSpeed(); }
    public void RemoveSpeedBonus(float bonus) { speedBonus -= bonus; UpdatePlayerSpeed(); }
    public void AddDamageBonus(float bonus) => damageBonus += bonus;
    public void RemoveDamageBonus(float bonus) => damageBonus -= bonus;
    public void AddArmorBonus(float bonus) => armorBonus = Mathf.Clamp01(armorBonus + bonus);
    public void RemoveArmorBonus(float bonus) => armorBonus = Mathf.Clamp01(armorBonus - bonus);
    public void AddCritChanceBonus(float bonus) => critChanceBonus = Mathf.Clamp01(critChanceBonus + bonus);
    public void RemoveCritChanceBonus(float bonus) => critChanceBonus = Mathf.Clamp01(critChanceBonus - bonus);

    private void UpdatePlayerSpeed()
    {
        if (photonView.IsMine)
        {
            PlayerMain player = GetComponent<PlayerMain>();
            if (player != null) player.speed = CurrentSpeed;
        }
        OnSpeedChanged?.Invoke();
    }

    public void ResetStats()
    {
        currentHealth = maxHealth;
        speedBonus = 0f;
        damageBonus = 0f;
        armorBonus = 0f;
        critChanceBonus = 0f;
        UpdatePlayerSpeed();
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 本地玩家：发送自己的当前血量
            stream.SendNext(currentHealth);
        }
        else
        {
            // 远程玩家：接收血量数据
            float receivedHealth = (float)stream.ReceiveNext();

            // 如果接收到的血量与当前显示的血量差异超过 1 点，则强制覆盖并刷新 UI
            if (Mathf.Abs(currentHealth - receivedHealth) > 1f)
            {
                currentHealth = receivedHealth;
                OnHealthChanged?.Invoke(currentHealth);
            }
        }
    }

}