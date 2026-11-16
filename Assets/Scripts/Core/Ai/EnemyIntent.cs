namespace ZeroWalker.Core.Ai
{
    /// <summary>
    /// 大脑每帧给出的「意图」：只说想怎么动、想不想出手，不碰刚体也不认识 Unity。
    /// Game 层的敌人组件负责把意图落实成速度、朝向、生成子弹。
    /// </summary>
    public struct EnemyIntent
    {
        /// <summary>期望的水平速度（世界单位/秒，0 表示站住）。</summary>
        public float MoveX;

        /// <summary>想要的朝向：1 向右、-1 向左、0 保持不动。</summary>
        public int Facing;

        /// <summary>本帧发起一次近战攻击判定（只发一次）。</summary>
        public bool Attack;

        /// <summary>本帧发射一颗子弹（只发一次）。</summary>
        public bool Shoot;
    }
}
