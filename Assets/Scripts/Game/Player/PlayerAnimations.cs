using ZeroWalker.Core.Model;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.Player
{
    /// <summary>
    /// Blitz 骨骼动画名称的取值入口。
    /// 真正的值在 Content/data/assets.json（资源 key 唯一登记处），这里只做转发：
    /// 改动画名改 JSON，换资源不用动代码。
    /// </summary>
    public static class PlayerAnimations
    {
        private static SpineAnimations Sheet
        {
            get { return ContentLoader.Current.assets.spine.animations; }
        }

        public static string Idle
        {
            get { return Sheet.idle; }
        }

        public static string Move
        {
            get { return Sheet.move; }
        }

        public static string AttackBegin
        {
            get { return Sheet.attackBegin; }
        }

        public static string AttackLoop
        {
            get { return Sheet.attackLoop; }
        }

        public static string AttackEnd
        {
            get { return Sheet.attackEnd; }
        }

        /// <summary>Attack_Loop 中标记“枪口开火”的 Spine 事件名。</summary>
        public static string AttackEvent
        {
            get { return Sheet.attackEvent; }
        }

        public static string Die
        {
            get { return Sheet.die; }
        }

        public static string Start
        {
            get { return Sheet.start; }
        }

        public static string Skill01
        {
            get { return Sheet.skill01; }
        }

        public static string Skill02
        {
            get { return Sheet.skill02; }
        }
    }
}
