using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Joystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("组件")]
    public RectTransform background;          // 摇杆背景（会移动）
    public RectTransform handle;              // 摇杆把手
    public RectTransform joystickArea;        // 新增：感应区域（父容器）

    [Header("参数")]
    public float handleRange = 1f;
    public float deadZone = 0.1f;

    private Vector2 inputDirection = Vector2.zero;
    private Canvas rootCanvas;
    private bool isDragging = false;
    private Vector2 backgroundOriginalPos;     // 背景初始位置（用于复位）

    void Start()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
            Debug.LogError("Joystick: 找不到父级 Canvas！");

        // 记录背景初始位置（相对于 Joystick 物体的 anchoredPosition）
        backgroundOriginalPos = background.anchoredPosition;

        // 如果没有手动赋值感应区域，尝试自动查找父物体
        if (joystickArea == null)
            joystickArea = transform.parent?.GetComponent<RectTransform>();
    }



    private void MoveBackgroundToTouch(PointerEventData eventData)
    {
        if (background == null || joystickArea == null) return;

        // 将屏幕触摸点转换为感应区域内的本地坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickArea,
            eventData.position,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
            out Vector2 localPoint
        );

        // 将背景移动到该位置（背景是 joystickArea 的子物体，所以直接设置 anchoredPosition）
        background.anchoredPosition = localPoint;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 移动背景到触摸位置
        MoveBackgroundToTouch(eventData);

        // 将把手归零（相对于背景中心）
        handle.anchoredPosition = Vector2.zero;

        isDragging = true;

        // 可选：立即发送一次零输入
        if (PlayerMain.Instance != null)
            PlayerMain.Instance.OnMoveInput(Vector2.zero);

        // 注意：这里不再调用 OnDrag，因为把手已在中心，且 OnDrag 会在手指移动时自动触发
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // 将触摸点转换为背景内的本地坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
            out Vector2 localPoint
        );

        float radius = background.rect.width * 0.5f;
        Vector2 rawDirection = Vector2.ClampMagnitude(localPoint, radius) / radius;

        Vector2 outputDirection;
        if (rawDirection.magnitude < deadZone)
            outputDirection = Vector2.zero;
        else
            outputDirection = rawDirection.normalized;

        inputDirection = outputDirection;

        Vector2 displayPosition = rawDirection * radius * handleRange;
        handle.anchoredPosition = displayPosition;

        if (PlayerMain.Instance != null)
            PlayerMain.Instance.OnMoveInput(inputDirection);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        inputDirection = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;

        // 复位背景位置
        background.anchoredPosition = backgroundOriginalPos;

        if (PlayerMain.Instance != null)
            PlayerMain.Instance.OnMoveInput(Vector2.zero);
    }

    public float Horizontal => inputDirection.x;
    public float Vertical => inputDirection.y;
}