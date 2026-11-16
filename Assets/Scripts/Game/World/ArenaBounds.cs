using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroWalker.Core.Model;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.World
{
    /// <summary>
    /// 可行走区域：横向 = 整个关卡有多宽，纵向 = 一条带子。
    ///
    /// - 横向不设数值：运行时自动收集背景块（名字以 <c>BG</c> 开头的 SpriteRenderer，
    ///   或都放在 <see cref="root"/> 下面），取它们的并集 0..100%。加一块背景，
    ///   关卡就自动变长，不用改 JSON、不用跑菜单。
    /// - 纵向来自 Content/data/arena.json 的 <c>minY / maxY</c>：相对单块背景图高度的比例，
    ///   正好框住画面里那条可走的桥面带。
    ///
    /// 背景块用 sprite.bounds 经 TransformPoint 换算（不是 renderer.bounds）：镜像块、
    /// 编辑器里刚挪过位置的情况都算得对。
    /// </summary>
    [DisallowMultipleComponent]
    public class ArenaBounds : MonoBehaviour
    {
        [Tooltip("可选：把所有背景块放在这个父物体下。留空则自动收集场景里名字以 BG 开头的 SpriteRenderer")]
        [SerializeField] private Transform root;

        [Tooltip("收集到的背景块数量会被打印出来吗？只在排查地图没接上时打开")]
        [SerializeField] private bool logTiles;

        private SpriteRenderer[] _tiles;

        private ArenaTuning Tuning
        {
            get { return ContentLoader.Current.arena; }
        }

        /// <summary>收集到的背景块（首次访问时扫描一次，之后复用）。</summary>
        public IReadOnlyList<SpriteRenderer> Tiles
        {
            get
            {
                if (_tiles == null)
                {
                    _tiles = ResolveTiles();
                }

                return _tiles;
            }
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

            for (var i = 0; i < list.Count; i++)
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
            var bottom = tileMinY + tileHeight * tuning.minY;
            var top = tileMinY + tileHeight * tuning.maxY;

            var center = new Vector3((minX + maxX) * 0.5f, (bottom + top) * 0.5f, 0f);
            var size = new Vector3(maxX - minX, top - bottom, 1f);
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

        /// <summary>重新扫描背景块（场景里加了新背景、又不想重进 Play 时用）。</summary>
        [ContextMenu("重新收集背景块")]
        public void RefreshTiles()
        {
            _tiles = null;
            var count = Tiles.Count;
            Debug.Log("[Arena] 重新收集到 " + count + " 块背景。");
        }

        private SpriteRenderer[] ResolveTiles()
        {
            var list = new List<SpriteRenderer>();

            if (root != null)
            {
                list.AddRange(root.GetComponentsInChildren<SpriteRenderer>(true));
            }
            else
            {
                list.AddRange(FindObjectsOfType<SpriteRenderer>(true));
            }

            list.RemoveAll(IsUsableBackground);

            // 按 x 排序，方便排查时对照场景
            list.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            if (logTiles)
            {
                Debug.Log("[Arena] 收集到 " + list.Count + " 块背景（横向取并集）。");
            }

            return list.ToArray();
        }

        private bool IsUsableBackground(SpriteRenderer candidate)
        {
            if (candidate == null || candidate.sprite == null)
            {
                return true;
            }

            // 指定了 root 就只认它下面的；否则按命名约定筛，避免把角色/特效也当成地图
            return root == null && !candidate.name.StartsWith("BG", StringComparison.OrdinalIgnoreCase);
        }

        private void OnValidate()
        {
            _tiles = null;
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
            _tiles = null;

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
