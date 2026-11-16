using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZeroWalker.Combat;
using ZeroWalker.Enemies;
using ZeroWalker.Player;
using ZeroWalker.VFX;

namespace ZeroWalker.EditorTools
{
    /// <summary>
    /// 用胶囊占位生成两种敌人，并把它们摆进当前场景：
    /// 近战型（巡逻 → 追击 → 前摇 → 扑击）与远程型（瞄准 → 开火 → 冷却）。
    /// 占位美术是脚本画出来的 PNG，正式素材到位后只换 Sprite，逻辑与预制体结构都不用改。
    /// 幂等：重复执行会重建预制体、替换场景里的旧实例。
    /// </summary>
    public static class EnemySetup
    {
        private const string SpriteFolder = "Assets/ArtRes/Enemies";
        private const string PrefabFolder = "Assets/Prefabs/Enemies";
        private const string MeleeSpritePath = SpriteFolder + "/Enemy_Melee.png";
        private const string RangedSpritePath = SpriteFolder + "/Enemy_Ranged.png";
        private const string MeleePrefabPath = PrefabFolder + "/MeleeEnemy.prefab";
        private const string RangedPrefabPath = PrefabFolder + "/RangedEnemy.prefab";
        private const string BulletPrefabPath = "Assets/Prefabs/Bullet.prefab";
        private const string HitEffectPrefabPath = "Assets/Prefabs/HitEffect.prefab";
        private const string ContainerName = "Enemies";

        private const int CapsuleWidth = 64;
        private const int CapsuleHeight = 96;
        private const float PixelsPerUnit = 100f;

        private static readonly Vector2 MeleeSpawn = new Vector2(5.0f, 0.1f);
        private static readonly Vector2 RangedSpawn = new Vector2(-5.0f, 0.9f);

        [MenuItem("ZeroWalker/生成敌人（胶囊占位）", priority = 30)]
        public static void Generate()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[EnemySetup] 播放模式下不能重建，请先停止播放。");
                return;
            }

            var createdTag = EnemyTagSetup.EnsureTag(GameTags.Enemy);

            EnsureFolder(SpriteFolder);
            EnsureFolder(PrefabFolder);

            var meleeSprite = EnsureCapsuleSprite(MeleeSpritePath, new Color32(226, 98, 78, 255), new Color32(118, 42, 36, 255));
            var rangedSprite = EnsureCapsuleSprite(RangedSpritePath, new Color32(170, 118, 226, 255), new Color32(82, 48, 118, 255));

            var bulletPrefab = AssetDatabase.LoadAssetAtPath<Bullet>(BulletPrefabPath);
            if (bulletPrefab == null)
                Debug.LogWarning("[EnemySetup] 找不到 " + BulletPrefabPath + "，远程敌人将打不出子弹。");

            var hitEffectPrefab = AssetDatabase.LoadAssetAtPath<HitEffect>(HitEffectPrefabPath);
            if (hitEffectPrefab == null)
                Debug.LogWarning("[EnemySetup] 找不到 " + HitEffectPrefabPath + "，近战扑击不会有命中特效。");

            var meleePrefab = BuildMelee(meleeSprite, hitEffectPrefab);
            var rangedPrefab = BuildRanged(rangedSprite, bulletPrefab);

            var scene = EditorSceneManager.GetActiveScene();
            var container = EnsureContainer();

            var melee = Place(meleePrefab, container.transform, "MeleeEnemy", MeleeSpawn);
            var ranged = Place(rangedPrefab, container.transform, "RangedEnemy", RangedSpawn);

            // 敌人子弹靠 Player 标签判断该不该命中
            var player = Object.FindObjectOfType<PlayerController>();
            if (player != null)
                player.gameObject.tag = GameTags.Player;
            else
                Debug.LogWarning("[EnemySetup] 场景里没有找到玩家，敌人子弹将无法命中。");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = melee != null ? melee : ranged;

            Debug.Log("[EnemySetup] 完成：Tag" + (createdTag ? " 已新建" : " 已存在")
                + "；近战敌人 " + (melee != null ? melee.transform.position.ToString() : "未放置")
                + "；远程敌人 " + (ranged != null ? ranged.transform.position.ToString() : "未放置")
                + "；玩家标签 = " + (player != null ? player.gameObject.tag : "无"));
        }

        private static GameObject BuildMelee(Sprite sprite, HitEffect hitEffectPrefab)
        {
            var root = BuildRoot("MeleeEnemy", sprite, EnemyKind.Melee);

            var agent = root.AddComponent<MeleeEnemy>();
            SetReference(agent, "visual", root.transform.Find("Visual"));
            SetReference(agent, "hitEffectPrefab", hitEffectPrefab);

            return SavePrefab(root, MeleePrefabPath);
        }

        private static GameObject BuildRanged(Sprite sprite, Bullet bulletPrefab)
        {
            var root = BuildRoot("RangedEnemy", sprite, EnemyKind.Ranged);

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0.45f, 0.5f, 0f);

            var agent = root.AddComponent<RangedEnemy>();
            SetReference(agent, "visual", root.transform.Find("Visual"));
            SetReference(agent, "muzzle", muzzle.transform);
            SetReference(agent, "bulletPrefab", bulletPrefab);

            return SavePrefab(root, RangedPrefabPath);
        }

        /// <summary>两种敌人共用的根：刚体、碰撞体、血量、视图、子物体 Visual。</summary>
        private static GameObject BuildRoot(string name, Sprite sprite, EnemyKind kind)
        {
            var root = new GameObject(name);
            root.tag = GameTags.Enemy;

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = root.AddComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Vertical;
            collider.size = new Vector2(0.55f, 0.9f);
            collider.offset = new Vector2(0f, 0.45f);
            collider.isTrigger = true;

            var health = root.AddComponent<EnemyHealth>();
            SetEnum(health, "kind", (int)kind);

            var view = root.AddComponent<EnemyView>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 1;

            var flash = visual.AddComponent<SpriteFlash>();
            SetReference(flash, "target", renderer);

            SetReference(view, "flash", flash);

            return root;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject EnsureContainer()
        {
            var container = GameObject.Find(ContainerName);
            if (container == null)
                container = new GameObject(ContainerName);

            // 去掉上一次生成的实例，避免重复执行后场景里堆一串敌人
            var old = new System.Collections.Generic.List<GameObject>();
            for (var i = container.transform.childCount - 1; i >= 0; i--)
            {
                var child = container.transform.GetChild(i).gameObject;
                if (child.name == "MeleeEnemy" || child.name == "RangedEnemy")
                    old.Add(child);
            }

            foreach (var go in old)
                Object.DestroyImmediate(go);

            return container;
        }

        private static GameObject Place(GameObject prefab, Transform parent, string name, Vector2 position)
        {
            if (prefab == null) return null;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.position = new Vector3(position.x, position.y, 0f);
            return instance;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        /// <summary>没有占位图就画一张胶囊（带描边），并设成 Sprite。</summary>
        private static Sprite EnsureCapsuleSprite(string path, Color32 fill, Color32 outline)
        {
            if (!File.Exists(path))
                File.WriteAllBytes(path, BuildCapsulePng(fill, outline));

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static byte[] BuildCapsulePng(Color32 fill, Color32 outline)
        {
            var texture = new Texture2D(CapsuleWidth, CapsuleHeight, TextureFormat.RGBA32, false);
            var pixels = new Color32[CapsuleWidth * CapsuleHeight];

            var centerX = CapsuleWidth * 0.5f;
            var radius = CapsuleWidth * 0.5f;
            var topY = CapsuleHeight - radius;
            var bottomY = radius;

            for (var y = 0; y < CapsuleHeight; y++)
            {
                for (var x = 0; x < CapsuleWidth; x++)
                {
                    var dx = x + 0.5f - centerX;
                    var dy = y + 0.5f - Mathf.Clamp(y + 0.5f, bottomY, topY);
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);

                    if (distance <= radius - 2f)
                        pixels[y * CapsuleWidth + x] = fill;
                    else if (distance <= radius)
                        pixels[y * CapsuleWidth + x] = outline;
                    else
                        pixels[y * CapsuleWidth + x] = new Color32(0, 0, 0, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var bytes = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return bytes;
        }

        private static void SetReference(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            var field = serialized.FindProperty(property);

            if (field == null)
            {
                Debug.LogWarning("[EnemySetup] " + target.GetType().Name + " 上找不到字段 " + property);
                return;
            }

            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string property, int value)
        {
            var serialized = new SerializedObject(target);
            var field = serialized.FindProperty(property);

            if (field == null)
            {
                Debug.LogWarning("[EnemySetup] " + target.GetType().Name + " 上找不到枚举 " + property);
                return;
            }

            field.enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
