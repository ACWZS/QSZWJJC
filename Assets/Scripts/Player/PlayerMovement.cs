using UnityEngine;

public static class PlayerMovement
{

    public static void UpdateMovement()
    {
        var player = PlayerMain.Instance;
        if (player == null) return;
        // 如果控制器未启用，直接返回（例如死亡后）
        if (player.Controller == null || !player.Controller.enabled) return;

        // 获取输入：优先使用 UI 摇杆，若摇杆无输入则使用键盘
        float x = player._moveInput.x;
        float z = player._moveInput.y;
        if (Mathf.Abs(x) < 0.1f && Mathf.Abs(z) < 0.1f)
        {
            x = Input.GetAxis("Horizontal");
            z = Input.GetAxis("Vertical");
        }

        // 相机相对方向
        Vector3 moveDirection = Vector3.zero;
        if (player.cameraTransform != null)
        {
            Vector3 cameraForward = player.cameraTransform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            Vector3 cameraRight = player.cameraTransform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();
            moveDirection = cameraRight * x + cameraForward * z;
            if (moveDirection.magnitude > 1f) moveDirection.Normalize();
        }

        // 移动
        player.Controller.Move(moveDirection * player.speed * Time.deltaTime);

        // 行走动画
        if (player.Animator != null)
            player.Animator.SetBool("isWalking", moveDirection.magnitude > 0.1f);

        // 跳跃：同时支持 UI 按钮和键盘空格
        bool jumpInput = player.JumpPressed || Input.GetButtonDown("Jump");
        if (jumpInput && player.IsGrounded)
        {
            player.Velocity = new Vector3(player.Velocity.x, Mathf.Sqrt(player.jumpHeight * -2f * PlayerMain.Gravity), player.Velocity.z);
            player.JumpPressed = false;
        }

        // 重力
        player.Velocity += Vector3.up * PlayerMain.Gravity * Time.deltaTime;
        player.Controller.Move(player.Velocity * Time.deltaTime);

        // 旋转：仅在非攻击状态下由移动方向控制
        if (!player.IsInContinuousAttack && !player.IsSingleShotPending)
        {
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDirection);
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRot, player.rotationSpeed * Time.deltaTime);
            }
        }
    }
}