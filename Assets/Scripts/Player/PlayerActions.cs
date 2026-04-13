using UnityEngine;
using System.Collections;

public class PlayerActions : MonoBehaviour
{
    public bool IsBusy { get; private set; }
    public bool IsHealing { get; private set; }
    public float CurrentProgress { get; private set; }

    private Health health;
    private Coroutine currentAction;
    private System.Action<float> onProgressCallback;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    public void StartHeal(float duration, float healAmount, bool isPercent,
                          System.Action onComplete = null,
                          System.Action<float> onProgress = null)
    {
        if (IsBusy) return;
        onProgressCallback = onProgress;
        if (currentAction != null) StopCoroutine(currentAction);
        currentAction = StartCoroutine(HealCoroutine(duration, healAmount, isPercent, onComplete));
    }

    private IEnumerator HealCoroutine(float duration, float healAmount, bool isPercent, System.Action onComplete)
    {
        IsBusy = true;
        IsHealing = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            CurrentProgress = elapsed / duration;
            onProgressCallback?.Invoke(CurrentProgress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        onProgressCallback?.Invoke(1f);
        if (health != null)
            health.Heal(healAmount, isPercent);

        IsBusy = false;
        IsHealing = false;
        onComplete?.Invoke();
        currentAction = null;
    }

    public void CancelAction()
    {
        if (currentAction != null)
        {
            StopCoroutine(currentAction);
            currentAction = null;
        }
        IsBusy = false;
        IsHealing = false;

        // 通知进度回调治疗被取消（进度=0）
        onProgressCallback?.Invoke(0f);
        onProgressCallback = null;
    }
}