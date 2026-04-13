using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private float moveSpeed = 1.5f;      // 上浮速度
    [SerializeField] private float fadeDuration = 0.8f;   // 淡出时长

    private Color startColor;
    private float timer;
    private DamagePopupManager manager;
    private Camera mainCamera;          // 新增：主相机引用

    public void Setup(float damage, bool isCrit, bool isHeal, DamagePopupManager mgr)
    {
        manager = mgr;
        mainCamera = Camera.main;       // 获取主相机

        textMesh.text = Mathf.RoundToInt(damage).ToString();
        textMesh.color = isHeal ? Color.green : (isCrit ? Color.yellow : Color.white);
        startColor = textMesh.color;
        timer = fadeDuration;

        // 初始时立刻面向相机一次，防止第一帧方向错误
        FaceCamera();
    }

    private void Update()
    {
        // 1. 向上移动
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 2. 始终面向相机（Billboard效果）
        FaceCamera();

        // 3. 淡出处理
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            gameObject.SetActive(false);
            manager?.ReturnToPool(gameObject);
            return;
        }

        Color c = startColor;
        c.a = Mathf.Clamp01(timer / fadeDuration);
        textMesh.color = c;
    }

    /// <summary>
    /// 使文字始终面向主摄像机
    /// </summary>
    private void FaceCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null)
        {
            // 让文字朝向相机，并旋转180度使文字正向（因为TextMeshPro默认的正面是相反的）
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180, 0);
        }
    }
}