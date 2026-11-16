using System.Collections.Generic;

namespace ZeroWalker.Core.Model
{
    /// <summary>
    /// 可行走区域的**纵向**band：对应 Content/data/arena.json。
    /// 两个值都是「单块背景高度的归一化比例」，原点在左下角：0 = 图底，1 = 图顶。
    ///
    /// 横向不在这里——横向就是地图本身有多宽（所有背景块的并集，见 ArenaBounds），
    /// 加一块背景关卡自动变长，不需要改任何数值。
    /// </summary>
    public sealed class ArenaTuning
    {
        /// <summary>下边界（背景高度的比例）。</summary>
        public float minY = 0.30f;

        /// <summary>上边界（背景高度的比例）。</summary>
        public float maxY = 0.60f;

        public void Validate(List<string> problems)
        {
            CheckUnit(problems, "arena.minY", minY);
            CheckUnit(problems, "arena.maxY", maxY);

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
