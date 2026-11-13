using System.Collections.Generic;

namespace ZeroWalker.Core.Model
{
    /// <summary>
    /// 一局游戏用到的全部内容数值，是运行时唯一的数据入口。
    /// 由 Framework 层的 ContentLoader 从 Content/data/*.json 填充。
    /// </summary>
    public sealed class GameContent
    {
        public PlayerTuning player = new PlayerTuning();

        public InputTuning input = new InputTuning();

        public BulletTuning bullet = new BulletTuning();

        public TargetTuning target = new TargetTuning();

        public CameraTuning camera = new CameraTuning();

        public EffectsTuning effects = new EffectsTuning();

        public AnimationTuning animation = new AnimationTuning();

        public MuzzleTuning muzzle = new MuzzleTuning();

        /// <summary>校验数值合法性，返回全部问题描述（空表示通过）。</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            player.Validate(problems);
            input.Validate(problems);
            bullet.Validate(problems);
            target.Validate(problems);
            return problems;
        }
    }
}
