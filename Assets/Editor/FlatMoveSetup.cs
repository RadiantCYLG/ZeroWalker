using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZeroWalker.Player;
using ZeroWalker.World;

namespace ZeroWalker.EditorTools
{
    /// <summary>
    /// 一键把当前场景配置成「无重力平面走位」：
    /// 1) 玩家刚体去重力、碰撞体改成触发器（走位只受活动范围约束，不被柱子/靶子挡住）
    /// 2) 把背景块登记进 ArenaBounds（横向并集），并让玩家引用它
    /// 3) 玩家若在活动范围外，挪回范围内
    ///
    /// 只改场景里的引用与开关，不重建场景、不动美术摆放，可以反复执行。
    /// 数值（走位速度、活动范围比例）不在这里，改 Content/data/*.json。
    /// </summary>
    public static class FlatMoveSetup
    {
        private const string MenuPath = "ZeroWalker/应用平面走位配置（W/S + 活动范围）";
        private const string BackgroundPrefix = "BG";

        [MenuItem(MenuPath)]
        public static void Apply()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[FlatMoveSetup] 没有打开的场景。");
                return;
            }

            var player = Object.FindObjectOfType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[FlatMoveSetup] 场景里找不到 PlayerController。");
                return;
            }

            PrepareBody(player);

            var tiles = CollectBackgroundTiles();
            if (tiles.Length == 0)
            {
                Debug.LogError("[FlatMoveSetup] 找不到背景块（没有名字以 \"BG\" 开头的 SpriteRenderer）。");
                return;
            }

            var arena = PrepareArena(tiles);
            SetPlayerArena(player, arena);
            PullPlayerInside(player, arena);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = arena.gameObject;

            Bounds bounds;
            if (arena.TryGetBounds(out bounds))
            {
                Debug.Log(string.Format(
                    "[FlatMoveSetup] 完成：{0} 块背景，活动范围 x[{1:0.00}, {2:0.00}] y[{3:0.00}, {4:0.00}]（改 Content/data/arena.json 调整）。",
                    tiles.Length, bounds.min.x, bounds.max.x, bounds.min.y, bounds.max.y));
            }
            else
            {
                Debug.Log("[FlatMoveSetup] 完成，但活动范围还没算出来（检查背景块的 Sprite 是否已赋值）。");
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateApply()
        {
            return !EditorApplication.isPlaying;
        }

        private static void PrepareBody(PlayerController player)
        {
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null && !Mathf.Approximately(body.gravityScale, 0f))
            {
                body.gravityScale = 0f;
                Debug.Log("[FlatMoveSetup] 玩家刚体重力已设为 0（平面走位不再下落）。");
            }

            var collider = player.GetComponent<Collider2D>();
            if (collider != null && !collider.isTrigger)
            {
                // 走位只受活动范围约束；实体碰撞会让角色卡在柱子/靶子上
                collider.isTrigger = true;
                Debug.Log("[FlatMoveSetup] 玩家碰撞体已改为触发器。");
            }
        }

        private static SpriteRenderer[] CollectBackgroundTiles()
        {
            var all = Object.FindObjectsOfType<SpriteRenderer>();
            var matched = new System.Collections.Generic.List<SpriteRenderer>();

            for (var i = 0; i < all.Length; i++)
            {
                var renderer = all[i];
                if (renderer.sprite == null) continue;
                if (!renderer.name.StartsWith(BackgroundPrefix, System.StringComparison.OrdinalIgnoreCase)) continue;
                matched.Add(renderer);
            }

            // 按 x 排序，方便在 Inspector 里看懂
            matched.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            return matched.ToArray();
        }

        private static ArenaBounds PrepareArena(SpriteRenderer[] tiles)
        {
            var host = tiles[0];
            var arena = host.GetComponent<ArenaBounds>();
            if (arena == null)
            {
                arena = host.gameObject.AddComponent<ArenaBounds>();
            }

            var serialized = new SerializedObject(arena);
            var property = serialized.FindProperty("tiles");
            property.arraySize = tiles.Length;
            for (var i = 0; i < tiles.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = tiles[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return arena;
        }

        private static void SetPlayerArena(PlayerController player, ArenaBounds arena)
        {
            var serialized = new SerializedObject(player);
            var property = serialized.FindProperty("arena");
            if (property == null)
            {
                Debug.LogError("[FlatMoveSetup] PlayerController 上没有 arena 字段。");
                return;
            }

            property.objectReferenceValue = arena;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PullPlayerInside(PlayerController player, ArenaBounds arena)
        {
            var position = (Vector2)player.transform.position;
            var clamped = arena.Clamp(position);
            if (clamped == position) return;

            player.transform.position = new Vector3(clamped.x, clamped.y, player.transform.position.z);
            Debug.Log("[FlatMoveSetup] 玩家起点在活动范围外，已挪到范围内。");
        }
    }
}
