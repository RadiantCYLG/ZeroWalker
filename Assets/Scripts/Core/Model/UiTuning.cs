using System.Collections.Generic;

namespace ZeroWalker.Core.Model
{
    /// <summary>
    /// 屏幕 UI 的尺寸（像素）。对应 Content/data/ui.json。
    /// 只放「尺寸/间距」这类可调数，颜色与图片在编辑器里配。
    /// </summary>
    public sealed class UiTuning
    {
        /// <summary>瞄准准星外圈圆环的直径（像素，按 Canvas 参考分辨率）。</summary>
        public float aimRingSize = 64f;

        /// <summary>准星中心圆点的直径（像素）。</summary>
        public float aimDotSize = 6f;

        public void Validate(List<string> problems)
        {
            if (aimRingSize <= 0f) problems.Add("ui.aimRingSize 必须大于 0");
            if (aimDotSize <= 0f) problems.Add("ui.aimDotSize 必须大于 0");
            if (aimDotSize > aimRingSize) problems.Add("ui.aimDotSize 不能大于 ui.aimRingSize");
        }
    }
}
