using UnityEngine;
using System.Collections;
using Photon.Pun;

public class EnemyAI : MonoBehaviour
{
    [Header("目标")]
    public Transform player;

    [Header("移动参数")]
    public float moveSpeed = 1f;
    public float rotationSpeed = 10f;
    public float chaseRange = 2f;
    public float attackRange = 1f;

    [Header("射击")]
    public Gun enemyGun;                 // 敌人使用的枪械组件

    [Header("枪械模型")]
    public GameObject gunModel;

    [Header("重生")]
    public Transform respawnPoint;
    public float respawnDelay = 3f;

    // 组件引用
    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private bool isGrounded;
    private Health health;
    private PlayerStats stats;          // 新增：属性组件
    private HealthBarWorld healthBar;
    private bool isDead = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
        stats = GetComponent<PlayerStats>();   // 获取 PlayerStats
        healthBar = GetComponentInChildren<HealthBarWorld>();

        if (health != null)
            health.OnDeath.AddListener(Die);

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            else Debug.LogWarning("未找到玩家，AI 将不会行动");
        }

        // 初始化武器：确保枪械知道自己的主人是当前 AI
        if (enemyGun != null)
        {
            // 设置枪械的 PhotonView 引用（如果是联机模式）
            // 这一步在 Gun.Start 中会自动从父物体查找，但显式设置更安全
            enemyGun.transform.root.TryGetComponent<PhotonView>(out var pv);
        }

        if (gunModel != null) gunModel.SetActive(false);
    }

    void Update()
    {
        if (player == null || isDead) return;

        // 简单重力
        isGrounded = controller.isGrounded;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            Attack();
        }
        else if (distanceToPlayer <= chaseRange)
        {
            Chase();
        }
        else
        {
            Idle();
        }

        // 应用重力
        if (isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // 掉落出世界
        if (transform.position.y < -10f)
        {
            Die();
        }
    }

    void Chase()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            controller.Move(direction * moveSpeed * Time.deltaTime);
        }
        SetAnimation(true, false);
        ShowGunModel(false);
    }

    void Attack()
    {
        // 面向玩家
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 射击：使用枪械的 TryFire 方法，它会自动处理子弹生成和伤害来源
        if (enemyGun != null)
        {
            enemyGun.TryFire();
        }

        SetAnimation(false, true);
        ShowGunModel(true);
    }

    void Idle()
    {
        SetAnimation(false, false);
        ShowGunModel(false);
    }

    void SetAnimation(bool isWalking, bool isAttacking)
    {
        if (animator != null)
        {
            animator.SetBool("isWalking", isWalking);
            animator.SetBool("isAttacking", isAttacking);
        }
    }

    void ShowGunModel(bool show)
    {
        if (gunModel != null && gunModel.activeSelf != show)
            gunModel.SetActive(show);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (controller != null) controller.enabled = false;
        if (animator != null) animator.enabled = false;
        if (enemyGun != null) enemyGun.enabled = false;
        ShowGunModel(false);

        velocity = Vector3.zero;
        StartCoroutine(RespawnCoroutine());
    }

    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }

        // 使用 Health.ResetHealth 来重置血量，它会调用 PlayerStats.ResetStats
        if (health != null)
            health.ResetHealth();

        // 手动更新血条
        if (healthBar != null && stats != null)
            healthBar.UpdateHealthBar(stats.CurrentHealth);

        if (controller != null) controller.enabled = true;
        if (animator != null) animator.enabled = true;
        if (enemyGun != null) enemyGun.enabled = true;

        isDead = false;
        velocity = Vector3.zero;
        SetAnimation(false, false);
    }
}