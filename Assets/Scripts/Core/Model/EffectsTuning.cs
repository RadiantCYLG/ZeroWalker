namespace ZeroWalker.Core.Model
{
    /// <summary>一次「闪光/缩放」特效的时长与缩放区间。</summary>
    public sealed class FlashTuning
    {
        /// <summary>特效持续时间（秒）。</summary>
        public float duration = 0.06f;

        /// <summary>起始缩放。</summary>
        public float startScale = 0.9f;

        /// <summary>结束缩放。</summary>
        public float endScale = 1.35f;
    }

    /// <summary>
    /// 纯表现类特效数值（不含玩法判定）。对应 Content/data/effects.json。
    /// SpriteFlash 默认使用 muzzleFlash 档；命中特效由 HitEffect 指定 hitFlash 档。
    /// </summary>
    public sealed class EffectsTuning
    {
        /// <summary>枪口火光。</summary>
        public FlashTuning muzzleFlash = new FlashTuning { duration = 0.06f, startScale = 0.9f, endScale = 1.35f };

        /// <summary>命中闪光。</summary>
        public FlashTuning hitFlash = new FlashTuning { duration = 0.15f, startScale = 0.7f, endScale = 1.5f };

        /// <summary>命中特效的默认叠加色。</summary>
        public RgbaColor hitEffectTint = new RgbaColor(0.35f, 0.95f, 1f, 1f);

        /// <summary>命中特效在闪光结束后的额外保留时间（秒）。</summary>
        public float hitEffectDestroyDelay = 0.02f;
    }

    /// <summary>Spine 动画播放参数。对应 Content/data/animation.json。</summary>
    public sealed class AnimationTuning
    {
        /// <summary>动作之间的默认过渡时间（秒）。</summary>
        public float defaultMix = 0.08f;
    }

    /// <summary>枪口位置参数。对应 Content/data/muzzle.json。</summary>
    public sealed class MuzzleTuning
    {
        /// <summary>Spine 骨骼名；找不到时退回 fallbackOffset。</summary>
        public string boneName = "F_Muzzle";

        /// <summary>找不到骨骼时相对角色的局部偏移。</summary>
        public Vec3 fallbackOffset = new Vec3(0.85f, 1.05f, 0f);
    }
}
