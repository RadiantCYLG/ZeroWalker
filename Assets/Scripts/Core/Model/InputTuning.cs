using System.Collections.Generic;

namespace ZeroWalker.Core.Model
{
    /// <summary>
    /// 玩家的按键绑定。对应 Content/data/input.json。
    /// 值是 Input System 的键名（Keyboard 按键名，如 "a"、"w"、"leftShift"；
    /// 鼠标键用按钮名，如 "leftButton"、"rightButton"），具体解析在 Game 层完成。
    /// </summary>
    public sealed class InputTuning
    {
        public string moveLeft = "a";
        public string moveRight = "d";
        public string moveUp = "w";
        public string moveDown = "s";
        public string dash = "leftShift";

        /// <summary>第二个冲刺键（与 dash 等价，按下即冲刺）。</summary>
        public string dashSecondary = "space";

        public string melee = "f";
        public string attack = "leftButton";

        /// <summary>瞄准键：按住时显示准星，射击朝准星方向。</summary>
        public string aim = "rightButton";

        public void Validate(List<string> problems)
        {
            RequireKey(problems, "input.moveLeft", moveLeft);
            RequireKey(problems, "input.moveRight", moveRight);
            RequireKey(problems, "input.moveUp", moveUp);
            RequireKey(problems, "input.moveDown", moveDown);
            RequireKey(problems, "input.dash", dash);
            RequireKey(problems, "input.dashSecondary", dashSecondary);
            RequireKey(problems, "input.melee", melee);
            RequireKey(problems, "input.attack", attack);
            RequireKey(problems, "input.aim", aim);
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
