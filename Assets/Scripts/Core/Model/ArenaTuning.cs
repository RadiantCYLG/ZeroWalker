using System.Collections.Generic;

namespace ZeroWalker.Core.Model
{
    /// <summary>
    /// 可行走区域的尺寸。对应 Content/data/arena.json。
    /// 四个值都是「背景图包围盒内的归一化比例」，原点在左下角：
    /// 0 = 背景最左/最下，1 = 最右/最上。这样背景缩放、挪动时活动范围自动跟着走。
    /// </summary>
    public sealed class ArenaTuning
    {
        /// <summary>左边界（背景宽度的比例）。</summary>
        public float minX = 0.012f;

        /// <summary>右边界（背景宽度的比例）。</summary>
        public float maxX = 0.985f;

        /// <summary>下边界（背景高度的比例）。</summary>
        public float minY = 0.266f;

        /// <summary>上边界（背景高度的比例）。</summary>
        public float maxY = 0.686f;

        public void Validate(List<string> problems)
        {
            CheckUnit(problems, "arena.minX", minX);
            CheckUnit(problems, "arena.maxX", maxX);
            CheckUnit(problems, "arena.minY", minY);
            CheckUnit(problems, "arena.maxY", maxY);

            if (minX >= maxX)
            {
                problems.Add("arena.minX 必须小于 arena.maxX");
            }

            if (minY >= maxY)
            {
                problems.Add("arena.minY 必须小于 arena.maxY");
            }
        }

        private static void CheckUnit(List<string> problems, string field, float value)
        {
            if (value < 0f || value > 1f)
            {
                problems.Add(field + " 必须在 [0, 1] 之间");
            }
        }
    }
}
