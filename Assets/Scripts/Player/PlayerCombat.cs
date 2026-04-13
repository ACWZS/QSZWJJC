using UnityEngine;

public static class PlayerCombat
{
    public static void UpdateCombat()
    {
        var player = PlayerMain.Instance;
        if (player == null) return;

        bool useMobile = false;
#if UNITY_EDITOR
        useMobile = player.simulateMobileInEditor;
#else
        useMobile = (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);
#endif

        // PC 端射击输入
#if UNITY_STANDALONE || UNITY_EDITOR
        if (!player.simulateMobileInEditor)
        {
            if (Input.GetKeyDown(player.fireKey))
                player.StartSingleAttack();
        }
#endif

        // 单次攻击动画结束
        if (player.IsSingleShotPending && Time.time >= player.AttackAnimEndTime)
            player.EndSingleShot();

        // 长按持续攻击结束（由 PlayerAiming 控制标志）
        if (player.IsInContinuousAttack && !player.IsLongPressConfirmed)
            player.EndContinuousAttack();

        // 应用目标旋转：仅在攻击状态下由瞄准模块控制
        if (player.IsInContinuousAttack || player.IsSingleShotPending)
        {
            Quaternion targetRotation = PlayerAiming.GetTargetRotation();
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRotation, player.rotationSpeed * Time.deltaTime);
        }
    }
}