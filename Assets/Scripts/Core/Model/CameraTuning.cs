namespace ZeroWalker.Core.Model
{
    /// <summary>2D 相机跟随的可调数值。对应 Content/data/camera.json。</summary>
    public sealed class CameraTuning
    {
        /// <summary>相对角色的偏移，z 必须为负才能把相机放在 2D 平面前方。</summary>
        public Vec3 offset = new Vec3(0f, 1.15f, -10f);

        /// <summary>平滑时间（秒），越小越跟手。</summary>
        public float smoothTime = 0.15f;

        /// <summary>是否锁定 Y 轴（只跟随水平方向）。</summary>
        public bool lockY;
    }
}
