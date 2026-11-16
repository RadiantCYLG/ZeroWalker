namespace ZeroWalker.Core.Ai
{
    /// <summary>
    /// 敌人这一帧「看到/知道」的信息，由 Game 层采集后交给大脑。
    /// 纯数据（没有引擎类型），让状态机可以脱离 Unity 单测。
    /// </summary>
    public struct EnemySenses
    {
        /// <summary>场景里是否还有玩家（玩家被打死/切场景时可能没有）。</summary>
        public bool HasPlayer;

        /// <summary>自己这一帧的位置。</summary>
        public float SelfX;
        public float SelfY;

        /// <summary>玩家这一帧的位置。</summary>
        public float PlayerX;
        public float PlayerY;

        /// <summary>自己到玩家的距离（由 Game 层算好，Core 不引引擎数学库）。</summary>
        public float PlayerDistance;

        /// <summary>玩家相对自己的水平差（正数＝在右边）。</summary>
        public float DeltaX
        {
            get { return PlayerX - SelfX; }
        }
    }
}
