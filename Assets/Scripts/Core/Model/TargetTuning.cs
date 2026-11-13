using System.Collections.Generic;

namespace ZeroWalker.Core.Model
{
    /// <summary>可被击中的靶子的表现数值。对应 Content/data/target.json。</summary>
    public sealed class TargetTuning
    {
        public int maxHealth = 3;

        /// <summary>受击闪色持续时间（秒）。</summary>
        public float hitFlashDuration = 0.08f;

        /// <summary>受击闪色叠加色。</summary>
        public RgbaColor hitColor = RgbaColor.White();

        /// <summary>死亡后是否自动复活。</summary>
        public bool respawn = true;

        /// <summary>复活等待时间（秒）。</summary>
        public float respawnDelay = 2f;

        /// <summary>死亡时的缩放倍数。</summary>
        public float deathScale = 0.6f;

        public void Validate(List<string> problems)
        {
            if (maxHealth <= 0)
            {
                problems.Add("target.maxHealth 必须大于 0");
            }

            if (hitFlashDuration < 0f)
            {
                problems.Add("target.hitFlashDuration 不能为负数");
            }

            if (respawnDelay < 0f)
            {
                problems.Add("target.respawnDelay 不能为负数");
            }

            if (deathScale <= 0f)
            {
                problems.Add("target.deathScale 必须大于 0");
            }
        }
    }
}
