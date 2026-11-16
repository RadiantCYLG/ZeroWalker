using System.Collections.Generic;

namespace ZeroWalker.Core.Model
{
    /// <summary>敌人数值总表。对应 Content/data/enemies.json。</summary>
    public sealed class EnemyTuning
    {
        public MeleeEnemyTuning melee = new MeleeEnemyTuning();

        public RangedEnemyTuning ranged = new RangedEnemyTuning();

        /// <summary>受击闪色（直接替换精灵颜色，染色要够亮才看得出）。</summary>
        public RgbaColor hitColor = new RgbaColor(1f, 0.55f, 0.55f, 1f);

        /// <summary>受击闪色持续时间（秒）。</summary>
        public float hitFlashDuration = 0.1f;

        /// <summary>倒地后多久销毁（秒）。给死亡表现留一点时间。</summary>
        public float deathDelay = 0.15f;

        public void Validate(List<string> problems)
        {
            melee.Validate(problems);
            ranged.Validate(problems);
            if (hitFlashDuration < 0f) problems.Add("enemies.hitFlashDuration 不能为负数");
            if (deathDelay < 0f) problems.Add("enemies.deathDelay 不能为负数");
        }
    }

    /// <summary>近战型地面敌人：在出生点附近巡逻，发现玩家后追击并扑击。</summary>
    public sealed class MeleeEnemyTuning
    {
        /// <summary>血量（子弹伤害固定 1 点）。</summary>
        public int maxHealth = 3;

        /// <summary>巡逻速度（世界单位/秒）。</summary>
        public float patrolSpeed = 1.2f;

        /// <summary>追击速度（发现玩家后）。</summary>
        public float chaseSpeed = 3.4f;

        /// <summary>巡逻范围：以出生点为中点，左右各多少米。</summary>
        public float patrolRange = 3f;

        /// <summary>发现玩家的半径。</summary>
        public float detectRadius = 5f;

        /// <summary>追丢的半径（要比发现半径大，避免玩家在边界来回时反复切状态）。</summary>
        public float loseRadius = 8f;

        /// <summary>进入扑击前摇的距离。</summary>
        public float attackRange = 1.3f;

        /// <summary>扑击过程中的命中判定半径。</summary>
        public float hitRadius = 1.6f;

        /// <summary>扑击前摇时间（给玩家躲避的窗口）。</summary>
        public float windupTime = 0.25f;

        /// <summary>扑击速度。</summary>
        public float lungeSpeed = 7f;

        /// <summary>扑击持续时间。</summary>
        public float lungeTime = 0.18f;

        /// <summary>扑击后的硬直时间。</summary>
        public float cooldown = 0.8f;

        public void Validate(List<string> problems)
        {
            if (maxHealth <= 0) problems.Add("enemies.melee.maxHealth 必须大于 0");
            if (patrolSpeed <= 0f) problems.Add("enemies.melee.patrolSpeed 必须大于 0");
            if (chaseSpeed <= 0f) problems.Add("enemies.melee.chaseSpeed 必须大于 0");
            if (patrolRange <= 0f) problems.Add("enemies.melee.patrolRange 必须大于 0");
            if (detectRadius <= 0f) problems.Add("enemies.melee.detectRadius 必须大于 0");
            if (loseRadius < detectRadius) problems.Add("enemies.melee.loseRadius 不能小于 detectRadius");
            if (attackRange <= 0f) problems.Add("enemies.melee.attackRange 必须大于 0");
            if (hitRadius < attackRange) problems.Add("enemies.melee.hitRadius 不能小于 attackRange");
            if (windupTime < 0f) problems.Add("enemies.melee.windupTime 不能为负数");
            if (lungeSpeed <= 0f) problems.Add("enemies.melee.lungeSpeed 必须大于 0");
            if (lungeTime <= 0f) problems.Add("enemies.melee.lungeTime 必须大于 0");
            if (cooldown < 0f) problems.Add("enemies.melee.cooldown 不能为负数");
        }
    }

    /// <summary>远程型敌人：固定不动，进入视野后瞄准并发射子弹。</summary>
    public sealed class RangedEnemyTuning
    {
        public int maxHealth = 2;

        /// <summary>发现玩家的半径。</summary>
        public float detectRadius = 8f;

        /// <summary>开火前摇（瞄准时间）。</summary>
        public float aimTime = 0.35f;

        /// <summary>两次射击之间的冷却。</summary>
        public float fireCooldown = 1.2f;

        /// <summary>子弹速度（覆盖 bullet.json 的 speed）。</summary>
        public float bulletSpeed = 12f;

        public void Validate(List<string> problems)
        {
            if (maxHealth <= 0) problems.Add("enemies.ranged.maxHealth 必须大于 0");
            if (detectRadius <= 0f) problems.Add("enemies.ranged.detectRadius 必须大于 0");
            if (aimTime < 0f) problems.Add("enemies.ranged.aimTime 不能为负数");
            if (fireCooldown < 0f) problems.Add("enemies.ranged.fireCooldown 不能为负数");
            if (bulletSpeed <= 0f) problems.Add("enemies.ranged.bulletSpeed 必须大于 0");
        }
    }
}
