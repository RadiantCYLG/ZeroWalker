using System.Collections.Generic;

namespace ZeroWalker.Core.Model
{
    /// <summary>
    /// 玩家移动、跳跃、冲刺的可调数值。对应 Content/data/player.json。
    /// 字段默认值与 JSON 保持一致，仅作「文件缺失/字段缺失」时的兜底，运行时以 JSON 为准。
    /// </summary>
    public sealed class PlayerTuning
    {
        /// <summary>水平移动速度（米/秒）。</summary>
        public float moveSpeed = 5f;

        /// <summary>起跳初速度。</summary>
        public float jumpForce = 9f;

        /// <summary>落地检测圆半径。</summary>
        public float groundCheckRadius = 0.12f;

        /// <summary>一次冲刺的总位移（米）。</summary>
        public float dashDistance = 4f;

        /// <summary>冲刺持续时间（秒）。</summary>
        public float dashDuration = 0.2f;

        /// <summary>冲刺冷却时间（秒）。</summary>
        public float dashCooldown = 1f;

        /// <summary>冲刺速度（米/秒），由「距离 ÷ 时长」推导，保证总位移等于 dashDistance。</summary>
        public float DashSpeed
        {
            get { return dashDistance / (dashDuration > 0.01f ? dashDuration : 0.01f); }
        }

        public void Validate(List<string> problems)
        {
            if (moveSpeed <= 0f)
            {
                problems.Add("player.moveSpeed 必须大于 0");
            }

            if (jumpForce <= 0f)
            {
                problems.Add("player.jumpForce 必须大于 0");
            }

            if (groundCheckRadius <= 0f)
            {
                problems.Add("player.groundCheckRadius 必须大于 0");
            }

            if (dashDistance <= 0f)
            {
                problems.Add("player.dashDistance 必须大于 0");
            }

            if (dashDuration <= 0f)
            {
                problems.Add("player.dashDuration 必须大于 0");
            }

            if (dashCooldown < 0f)
            {
                problems.Add("player.dashCooldown 不能为负数");
            }
        }
    }
}
