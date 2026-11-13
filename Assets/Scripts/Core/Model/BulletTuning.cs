using System.Collections.Generic;

namespace ZeroWalker.Core.Model
{
    /// <summary>子弹的外观与运动数值。对应 Content/data/bullet.json。</summary>
    public sealed class BulletTuning
    {
        /// <summary>飞行速度（米/秒）。</summary>
        public float speed = 22f;

        /// <summary>存活时间（秒），到时自动销毁。</summary>
        public float lifeTime = 2f;

        /// <summary>命中伤害。</summary>
        public int damage = 1;

        /// <summary>普通弹颜色。</summary>
        public RgbaColor normalColor = RgbaColor.White();

        /// <summary>特殊弹（攻击连射的蓄力弹）颜色。</summary>
        public RgbaColor specialColor = new RgbaColor(0.35f, 0.95f, 1f, 1f);

        /// <summary>特殊弹的缩放倍数。</summary>
        public float specialScale = 1.2f;

        public void Validate(List<string> problems)
        {
            if (speed <= 0f)
            {
                problems.Add("bullet.speed 必须大于 0");
            }

            if (lifeTime <= 0f)
            {
                problems.Add("bullet.lifeTime 必须大于 0");
            }

            if (damage <= 0)
            {
                problems.Add("bullet.damage 必须大于 0");
            }

            if (specialScale <= 0f)
            {
                problems.Add("bullet.specialScale 必须大于 0");
            }
        }
    }
}
