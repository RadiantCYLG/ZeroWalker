using System.Collections.Generic;

namespace ZeroWalker.Core.Model
{
    /// <summary>
    /// 玩家的按键绑定。对应 Content/data/input.json。
    /// 值是 Input System 的键名（Keyboard 按键名，如 "a"、"space"、"leftShift"；
    /// 攻击键用鼠标按钮名，如 "leftButton"），具体解析在 Game 层完成。
    /// </summary>
    public sealed class InputTuning
    {
        public string moveLeft = "a";
        public string moveRight = "d";
        public string jump = "space";
        public string dash = "leftShift";
        public string melee = "f";
        public string attack = "leftButton";

        public void Validate(List<string> problems)
        {
            RequireKey(problems, "input.moveLeft", moveLeft);
            RequireKey(problems, "input.moveRight", moveRight);
            RequireKey(problems, "input.jump", jump);
            RequireKey(problems, "input.dash", dash);
            RequireKey(problems, "input.melee", melee);
            RequireKey(problems, "input.attack", attack);
        }

        private static void RequireKey(List<string> problems, string field, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                problems.Add(field + " 不能为空");
            }
        }
    }
}
