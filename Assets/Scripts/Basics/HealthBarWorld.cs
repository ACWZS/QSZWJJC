using UnityEngine;
using UnityEngine.UI;

public class HealthBarWorld : MonoBehaviour
{
    [Header("目标")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 0.4f, 0);

    [Header("UI 组件")]
    public Image fillImage;

    private Health health;
    private PlayerStats stats;        // 新增引用
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogError("未找到主相机！");

        if (target != null)
        {
            health = target.GetComponent<Health>();
            stats = target.GetComponent<PlayerStats>();   // 获取 PlayerStats

            if (health == null)
                Debug.LogError($"目标 {target.name} 没有 Health 组件！");
            else if (stats == null)
                Debug.LogError($"目标 {target.name} 没有 PlayerStats 组件！");
            else
            {
                // 订阅带参数的事件
                health.OnHealthChanged.AddListener(UpdateHealthBar);
                // 初始更新
                UpdateHealthBar(stats.CurrentHealth);
            }
        }
        else
        {
            Debug.LogError("未指定跟随目标！");
        }
    }

    void LateUpdate()
    {
        if (target != null && mainCamera != null)
        {
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180, 0);
            transform.position = target.position + offset;
        }
    }

    // 修改：接受 float 参数
    public void UpdateHealthBar(float currentHealth)
    {
        if (stats != null && fillImage != null)
        {
            fillImage.fillAmount = currentHealth / stats.MaxHealth;
        }
    }

    // 无参版本保留兼容（但不会被调用）
    public void UpdateHealthBar()
    {
        if (stats != null)
            UpdateHealthBar(stats.CurrentHealth);
    }

    void OnDestroy()
    {
        if (health != null)
            health.OnHealthChanged.RemoveListener(UpdateHealthBar);
    }
}