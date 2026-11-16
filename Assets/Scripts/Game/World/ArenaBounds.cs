using UnityEngine;
using ZeroWalker.Core.Model;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.World
{
    /// <summary>
    /// 可行走区域：把「背景图包围盒」按 Content/data/arena.json 的四个归一化比例，
    /// 换算成世界坐标下的一条矩形带子，玩家只能在这条带子里活动。
    ///
    /// 横向取所有背景块的并集（关卡由几块图拼起来就拖几块进来），
    /// 纵向取第一块的高度（拼图时每块高度相同）。这样背景挪动、缩放、加块数，
    /// 活动范围都自动跟着走，不需要再改代码或场景。
    ///
    /// 挂在背景块之一上即可；「tiles」留空时退回用同一对象上的 SpriteRenderer。
    /// </summary>
    [DisallowMultipleComponent]
    public class ArenaBounds : MonoBehaviour
    {
        [Tooltip("组成关卡的背景块（横向取并集，纵向按第一块）。留空则用同一对象上的 SpriteRenderer")]
        [SerializeField] private SpriteRenderer[] tiles;

        private SpriteRenderer[] Tiles
        {
            get
            {
                if (tiles == null || tiles.Length == 0)
                {
                    tiles = new[] { GetComponent<SpriteRenderer>() };
                }

                return tiles;
            }
        }

        private ArenaTuning Tuning
        {
            get { return ContentLoader.Current.arena; }
        }

        /// <summary>矩形带子的中心点（世界坐标）。</summary>
        public Vector2 Center
        {
            get
            {
                Bounds bounds;
                return TryGetBounds(out bounds) ? (Vector2)bounds.center : (Vector2)transform.position;
            }
        }

        /// <summary>按当前背景与 arena.json 计算活动范围；没有可用背景块时返回 false。</summary>
        public bool TryGetBounds(out Bounds bounds)
        {
            bounds = default(Bounds);

            var list = Tiles;
            var found = false;
            var minX = 0f;
            var maxX = 0f;
            var tileMinY = 0f;
            var tileHeight = 0f;

            for (var i = 0; i < list.Length; i++)
            {
                Vector2 tileMin, tileMax;
                if (list[i] == null || !TryGetSpriteRect(list[i], out tileMin, out tileMax))
                {
                    continue;
                }

                if (!found)
                {
                    // 纵向基准取第一块能算出来的图：拼图时每块高度一致
                    tileMinY = tileMin.y;
                    tileHeight = tileMax.y - tileMin.y;
                    minX = tileMin.x;
                    maxX = tileMax.x;
                    found = true;
                    continue;
                }

                minX = Mathf.Min(minX, tileMin.x);
                maxX = Mathf.Max(maxX, tileMax.x);
            }

            if (!found || maxX <= minX || tileHeight <= 0f)
            {
                return false;
            }

            var tuning = Tuning;
            var spanX = maxX - minX;

            var left = minX + spanX * tuning.minX;
            var right = minX + spanX * tuning.maxX;
            var bottom = tileMinY + tileHeight * tuning.minY;
            var top = tileMinY + tileHeight * tuning.maxY;

            var center = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
            var size = new Vector3(right - left, top - bottom, 1f);
            bounds = new Bounds(center, size);
            return true;
        }

        /// <summary>把一个点夹进活动范围（玩家每物理帧调用）。</summary>
        public Vector2 Clamp(Vector2 point)
        {
            Bounds bounds;
            if (!TryGetBounds(out bounds))
            {
                return point;
            }

            return new Vector2(
                Mathf.Clamp(point.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(point.y, bounds.min.y, bounds.max.y));
        }

        /// <summary>
        /// 取一块背景在世界坐标下的左下/右上角。
        /// 用 sprite.bounds 经 TransformPoint 换算，而不是 renderer.bounds：
        /// 在编辑器脚本里刚挪过位置就立刻读 renderer.bounds，拿到的可能是上一帧的旧值；
        /// 这么做同时也照顾了镜像块（localScale.x 为负）。
        /// </summary>
        private static bool TryGetSpriteRect(SpriteRenderer renderer, out Vector2 min, out Vector2 max)
        {
            min = default(Vector2);
            max = default(Vector2);

            if (renderer == null || renderer.sprite == null)
            {
                return false;
            }

            var spriteBounds = renderer.sprite.bounds;
            var transform = renderer.transform;
            var cornerA = transform.TransformPoint(spriteBounds.min);
            var cornerB = transform.TransformPoint(spriteBounds.max);

            min = new Vector2(Mathf.Min(cornerA.x, cornerB.x), Mathf.Min(cornerA.y, cornerB.y));
            max = new Vector2(Mathf.Max(cornerA.x, cornerB.x), Mathf.Max(cornerA.y, cornerB.y));
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Bounds bounds;
            if (!TryGetBounds(out bounds))
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.92f, 0.3f, 0.9f);
            Gizmos.DrawWireCube(bounds.center, new Vector3(bounds.size.x, bounds.size.y, 0f));
        }
    }
}
