using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public static class PlayerAiming
{
    // 获取攻击方向（用于射击）
    public static Vector3 GetAttackDirection()
    {
        var player = PlayerMain.Instance;
        if (player == null) return Vector3.forward;

        bool useMobile = false;
#if UNITY_EDITOR
        useMobile = player.simulateMobileInEditor;
#else
        useMobile = (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);
#endif

        // 自由瞄准优先（移动端拖动瞄准线）
        if (useMobile && player.IsAiming && player.AimDirection.magnitude > 0f)
            return player.AimDirection;

        // 自动瞄准：找最近的敌人（受 autoAimRange 限制）
        Transform nearest = GetNearestEnemy(player.autoAimRange);
        if (nearest != null)
        {
            Vector3 dir = (nearest.position - player.transform.position).normalized;
            return dir;
        }

        Vector3 forward = player.transform.forward;
        forward.y = 0;
        if (forward.magnitude < 0.01f) forward = Vector3.forward;
        return forward.normalized;
    }

    // 获取目标旋转（用于角色转向）
    public static Quaternion GetTargetRotation()
    {
        var player = PlayerMain.Instance;
        if (player == null) return Quaternion.identity;

        if (player.IsInContinuousAttack || player.IsSingleShotPending)
        {
            Vector3 attackDir = GetAttackDirection();
            if (attackDir.magnitude > 0.1f)
            {
                Vector3 horizontalDir = attackDir;
                horizontalDir.y = 0;
                if (horizontalDir.magnitude > 0.1f)
                    return Quaternion.LookRotation(horizontalDir);
            }
        }
        return player.transform.rotation;
    }

    // 辅助瞄准修正（将当前瞄准方向向附近的敌人方向吸附）
    public static Vector3 GetAimAssistedDirection(Vector3 currentDirection, Vector3 playerPos, float radius, float angle, float smooth)
    {
        Transform bestTarget = GetBestTargetInCone(currentDirection, playerPos, radius, angle);
        if (bestTarget == null)
            return currentDirection;

        Vector3 targetDir = (bestTarget.position - playerPos).normalized;
        Vector3 newDir = Vector3.Slerp(currentDirection, targetDir, 1f - smooth);
        return newDir.normalized;
    }

    // 获取锥形范围内的最佳敌人（用于辅助瞄准）
    private static Transform GetBestTargetInCone(Vector3 direction, Vector3 playerPos, float radius, float angle)
    {
        List<Transform> enemies = GetAllEnemies();
        Transform best = null;
        float bestScore = 0f;

        foreach (Transform enemy in enemies)
        {
            if (enemy == null) continue;
            Vector3 toEnemy = enemy.position - playerPos;
            float dist = toEnemy.magnitude;
            if (dist > radius) continue;

            float angleToEnemy = Vector3.Angle(direction, toEnemy.normalized);
            if (angleToEnemy > angle) continue;

            float score = (1f - angleToEnemy / angle) * (1f - dist / radius);
            if (score > bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }
        return best;
    }

    // 获取最近的敌人（用于自动瞄准）
    public static Transform GetNearestEnemy(float maxRange = float.MaxValue)
    {
        var player = PlayerMain.Instance;
        if (player == null) return null;

        List<Transform> enemies = GetAllEnemies();
        float nearestDist = maxRange;
        Transform nearest = null;
        Vector3 myPos = player.transform.position;

        foreach (Transform enemy in enemies)
        {
            if (enemy == null) continue;
            float dist = Vector3.Distance(myPos, enemy.position);
            if (dist <= nearestDist)
            {
                nearestDist = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }

    // 获取所有潜在的敌人（AI 敌人 + 其他玩家）
    private static List<Transform> GetAllEnemies()
    {
        List<Transform> enemies = new List<Transform>();

        // 1. 获取所有标签为 "Enemy" 的 AI 敌人
        GameObject[] aiEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in aiEnemies)
        {
            if (enemy != null)
                enemies.Add(enemy.transform);
        }

        // 2. 获取所有标签为 "Player" 的玩家，排除本地玩家
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player == null) continue;
            // 排除本地玩家
            if (PlayerMain.Instance != null && player == PlayerMain.Instance.gameObject)
                continue;
            // 联机模式下，排除同队伍的玩家（如果未来有队友系统，可以在这里判断）
            // 暂时将所有其他玩家视为敌人
            enemies.Add(player.transform);
        }

        return enemies;
    }

    // 更新瞄准线（移动端）
    public static void UpdateAiming()
    {
        var player = PlayerMain.Instance;
        if (player == null) return;

        bool useMobile = false;
#if UNITY_EDITOR
        useMobile = player.simulateMobileInEditor;
#else
        useMobile = (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);
#endif

        if (useMobile && player.IsLongPressConfirmed && player.aimLine != null)
        {
            player.aimLine.enabled = true;
            Vector3 startPos = player.transform.position + player.aimLineOffset;
            Vector3 endPos = startPos + player.AimDirection * player.aimLineDistance;
            player.aimLine.SetPosition(0, startPos);
            player.aimLine.SetPosition(1, endPos);
        }
        else if (player.aimLine != null)
        {
            player.aimLine.enabled = false;
        }
    }

    // 兼容旧调用的重载（无参数版本）
    public static Transform GetNearestEnemy()
    {
        var player = PlayerMain.Instance;
        if (player == null) return null;
        return GetNearestEnemy(player.autoAimRange);
    }
}