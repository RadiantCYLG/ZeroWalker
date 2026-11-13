using System.Collections.Generic;

namespace ZeroWalker.Core.Model
{
    /// <summary>
    /// 资源 key 登记表。对应 Content/data/assets.json。
    /// 「用名字去找资源」的 key（动画名、骨骼名、将来的音效/特效 key）只写在这里，
    /// 代码里不裸写字符串，换资源只改这张表。
    /// </summary>
    public sealed class AssetsTuning
    {
        /// <summary>Spine 运行时资源（骨骼动画）的 key。</summary>
        public SpineAssets spine = new SpineAssets();

        public void Validate(List<string> problems)
        {
            spine.Validate(problems);
        }
    }

    /// <summary>Spine 骨架内可直接用名字定位的东西。</summary>
    public sealed class SpineAssets
    {
        /// <summary>动画名表。</summary>
        public SpineAnimations animations = new SpineAnimations();

        /// <summary>枪口骨骼名；骨架里找不到就退回 muzzle.json 的 fallbackOffset。</summary>
        public string muzzleBone = "F_Muzzle";

        public void Validate(List<string> problems)
        {
            if (string.IsNullOrEmpty(muzzleBone))
            {
                problems.Add("assets.spine.muzzleBone 不能为空");
            }

            animations.Validate(problems);
        }
    }

    /// <summary>Blitz 用到的 Spine 动画名与事件名。</summary>
    public sealed class SpineAnimations
    {
        public string idle = "Idle";
        public string move = "Move";
        public string attackBegin = "Attack_Begin";
        public string attackLoop = "Attack_Loop";
        public string attackEnd = "Attack_End";

        /// <summary>Attack_Loop 中标记「枪口开火」的 Spine 事件名。</summary>
        public string attackEvent = "OnAttack";

        public string die = "Die";
        public string start = "Start";
        public string skill01 = "Skill_01";
        public string skill02 = "Skill_02";

        public void Validate(List<string> problems)
        {
            RequireName(problems, "idle", idle);
            RequireName(problems, "move", move);
            RequireName(problems, "attackBegin", attackBegin);
            RequireName(problems, "attackLoop", attackLoop);
            RequireName(problems, "attackEnd", attackEnd);
            RequireName(problems, "attackEvent", attackEvent);
            RequireName(problems, "die", die);
            RequireName(problems, "start", start);
            RequireName(problems, "skill01", skill01);
            RequireName(problems, "skill02", skill02);
        }

        private static void RequireName(List<string> problems, string field, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                problems.Add("assets.spine.animations." + field + " 不能为空");
            }
        }
    }
}
