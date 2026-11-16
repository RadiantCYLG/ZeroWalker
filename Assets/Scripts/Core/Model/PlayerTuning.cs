using System.Collections.Generic;

namespace ZeroWalker.Core.Model
{
    /// <summary>
    /// 玩家移动、冲刺的可调数值。对应 Content/data/player.json。
    /// 角色在桥面这条「带子」上做八向移动：A/D 左右（X），W/S 纵深（Y）。
    /// 字段默认值与 JSON 保持一致，仅作「文件缺失/字段缺失」时的兜底，运行时以 JSON 为准。
    /// </summary>
    public sealed class PlayerTuning
    {
        /// <summary>左右移动速度（米/秒）。</summary>
        public float moveSpeed = 5f;

        /// <summary>纵深（上下）移动速度（米/秒）。斜视角下比左右慢一些更自然。</summary>
        public float moveSpeedY = 3.5f;

        /// <summary>一次冲刺的总位移（米）。</summary>
        public float dashDistance = 4f;

        /// <summary>冲刺持续时间（秒）。</summary>
        public float dashDuration = 0.2f;

        /// <summary>冲刺冷却时间（秒）。</summary>
        public float dashCooldown = 0.7f;

        /// <summary>移动输入死区：轴向量的长度不超过它视为「没有移动输入」。</summary>
        public float moveDeadzone = 0.01f;

        /// <summary>近战每次命中造成的伤害。</summary>
        public int meleeDamage = 1;

        /// <summary>近战判定盒的长度（米，朝角色正面伸出）。盒子的高度由它推导，不再单独开参数。</summary>
        public float meleeRange = 1.4f;

        /// <summary>从按下近战到判定生效的时间（秒）。让「刀落下」和「掉血」对齐，而不是一按就到。</summary>
        public float meleeHitDelay = 0.12f;

        /// <summary>
        /// 瞄准的最大仰角/俯角（度，相对水平方向，取绝对值）。
        /// 准星越过头顶时仰角会趋近 90°，这里把它夹到上限，子弹打不出「垂直朝天」。
        /// 必须小于 90（等于 90 就会有朝正上方的一发）。
        /// </summary>
        public float aimMaxAngle = 55f;

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

            if (moveSpeedY <= 0f)
            {
                problems.Add("player.moveSpeedY 必须大于 0");
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

            if (moveDeadzone < 0f || moveDeadzone >= 1f)
            {
                problems.Add("player.moveDeadzone 必须在 [0, 1) 之间");
            }

            if (meleeDamage < 1)
            {
                problems.Add("player.meleeDamage 至少为 1");
            }

            if (meleeRange <= 0f)
            {
                problems.Add("player.meleeRange 必须大于 0");
            }

            if (meleeHitDelay < 0f)
            {
                problems.Add("player.meleeHitDelay 不能为负数");
            }

            if (aimMaxAngle <= 0f || aimMaxAngle >= 90f)
            {
                problems.Add("player.aimMaxAngle 必须在 (0, 90) 之间");
            }
        }
    }
}
