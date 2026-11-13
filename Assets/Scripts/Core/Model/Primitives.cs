using System;

namespace ZeroWalker.Core.Model
{
    /// <summary>
    /// 与 JSON 一一对应的颜色值。Core 层不引用 UnityEngine，故自带一个纯数据颜色类型，
    /// 由 Framework 层的转换方法负责映射到 UnityEngine.Color。
    /// </summary>
    [Serializable]
    public sealed class RgbaColor
    {
        public float r = 1f;
        public float g = 1f;
        public float b = 1f;
        public float a = 1f;

        public RgbaColor()
        {
        }

        public RgbaColor(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static RgbaColor White()
        {
            return new RgbaColor(1f, 1f, 1f, 1f);
        }

        public RgbaColor Clone()
        {
            return new RgbaColor(r, g, b, a);
        }
    }

    /// <summary>与 JSON 一一对应的三维向量（偏移量等纯数值）。</summary>
    [Serializable]
    public sealed class Vec3
    {
        public float x;
        public float y;
        public float z;

        public Vec3()
        {
        }

        public Vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}
