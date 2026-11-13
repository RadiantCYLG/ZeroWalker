using UnityEngine;
using ZeroWalker.Core.Model;

namespace ZeroWalker.Framework.Content
{
    /// <summary>
    /// Core 层纯数据 → UnityEngine 类型的转换。放在 Framework 层，
    /// 保证 Core 不引用引擎、Game 层不必重复写转换代码。
    /// </summary>
    public static class TuningConvert
    {
        public static Color ToColor(this RgbaColor value)
        {
            return value == null ? Color.white : new Color(value.r, value.g, value.b, value.a);
        }

        public static Vector3 ToVector3(this Vec3 value)
        {
            return value == null ? Vector3.zero : new Vector3(value.x, value.y, value.z);
        }
    }
}
