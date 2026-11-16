using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ZeroWalker.Framework.Content;
using ZeroWalker.Player;
using ZeroWalker.UI;

namespace ZeroWalker.EditorTools
{
    /// <summary>
    /// 生成瞄准准星 UI（UGUI）：一个白色圆环 + 中心圆点，按住瞄准键时跟着鼠标。
    /// 图是脚本画的白图（纯白 + 透明度），换颜色/换造型只换图，不动代码。
    ///
    /// 组件挂在**画布**上（常驻激活），准星对象本身只切 Image.enabled；
    /// 早期版本把组件挂在准星上并 SetActive(false)，对象一关 Update 就不再跑，准星永远出不来。
    /// 幂等：重复执行会复用 Canvas、重建 AimReticle。
    /// </summary>
    public static class AimReticleSetup
    {
        private const string SpriteFolder = "Assets/ArtRes/UI";
        private const string RingPath = SpriteFolder + "/ReticleRing.png";
        private const string DotPath = SpriteFolder + "/ReticleDot.png";
        private const string CanvasName = "HUD";
        private const string ReticleName = "AimReticle";

        private const int RingSize = 128;
        private const int RingThickness = 6;
        private const int DotSize = 16;

        [MenuItem("ZeroWalker/生成瞄准准星 UI（UGUI）", priority = 40)]
        public static void Generate()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[AimReticleSetup] 播放模式下不能重建，请先停止播放。");
                return;
            }

            EnsureFolder(SpriteFolder);

            var ringSprite = EnsureSprite(RingPath, BuildRingPng());
            var dotSprite = EnsureSprite(DotPath, BuildDotPng());

            var canvas = EnsureCanvas();
            RemoveOld(ReticleName);

            var reticle = BuildReticle(canvas.transform, ringSprite, dotSprite, out var ringImage, out var dotImage);
            AttachView(canvas.gameObject, (RectTransform)reticle.transform, ringImage, dotImage, canvas);

            // 准星对象保持激活，只把两张图关掉：显隐由 AimReticleView 每帧切 enabled。
            ringImage.enabled = false;
            dotImage.enabled = false;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Selection.activeGameObject = reticle;

            var tuning = ContentLoader.Current.ui;
            Debug.Log("[AimReticleSetup] 完成：准星外圈 " + tuning.aimRingSize + "px、中心点 "
                + tuning.aimDotSize + "px；按住瞄准键显示，位置取自鼠标。");
        }

        /// <summary>取（或建）Overlay 画布；已有任意 Canvas 时复用它，避免出现第二个 HUD。</summary>
        private static Canvas EnsureCanvas()
        {
            var existing = Object.FindObjectOfType<Canvas>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static GameObject BuildReticle(
            Transform parent,
            Sprite ringSprite,
            Sprite dotSprite,
            out Image ringImage,
            out Image dotImage)
        {
            var reticle = new GameObject(ReticleName, typeof(RectTransform), typeof(Image));
            reticle.transform.SetParent(parent, false);

            var rootRect = (RectTransform)reticle.transform;
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            ringImage = reticle.GetComponent<Image>();
            ringImage.sprite = ringSprite;
            ringImage.color = Color.white;
            ringImage.raycastTarget = false;

            var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(reticle.transform, false);

            var dotRect = (RectTransform)dot.transform;
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = Vector2.zero;

            dotImage = dot.GetComponent<Image>();
            dotImage.sprite = dotSprite;
            dotImage.color = Color.white;
            dotImage.raycastTarget = false;

            ApplyPreviewSize(rootRect, dotRect);

            return reticle;
        }

        /// <summary>
        /// 把 AimReticleView 挂到画布上（画布常驻激活，才能每帧把准星摆到鼠标处）。
        /// 之前挂在准星上或重复跑过菜单时，清掉旧组件再挂，避免两个实例抢着改位置。
        /// </summary>
        private static void AttachView(
            GameObject canvasObject,
            RectTransform rootRect,
            Image ringImage,
            Image dotImage,
            Canvas canvas)
        {
            var view = canvasObject.GetComponent<AimReticleView>();
            if (view == null)
            {
                view = canvasObject.AddComponent<AimReticleView>();
            }

            // 准星对象上残留的旧实例（旧版菜单留下的）要删掉
            var stale = rootRect.GetComponent<AimReticleView>();
            if (stale != null)
            {
                Object.DestroyImmediate(stale);
            }

            var input = Object.FindObjectOfType<PlayerInputReader>();
            if (input == null)
            {
                Debug.LogWarning("[AimReticleSetup] 场景里没有找到 PlayerInputReader，准星不会跟随鼠标。");
            }

            SetReference(view, "root", rootRect);
            SetReference(view, "ring", ringImage);
            SetReference(view, "dot", dotImage);
            SetReference(view, "canvas", canvas);
            SetReference(view, "input", input);
        }

        /// <summary>编辑器里就按 ui.json 摆好尺寸，免得摆放时才看出来大小不对。</summary>
        private static void ApplyPreviewSize(RectTransform ring, RectTransform dot)
        {
            var tuning = ContentLoader.Current.ui;
            ring.sizeDelta = new Vector2(tuning.aimRingSize, tuning.aimRingSize);
            dot.sizeDelta = new Vector2(tuning.aimDotSize, tuning.aimDotSize);
        }

        private static void RemoveOld(string name)
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var old = canvas.transform.Find(name);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        /// <summary>没有图就画一张，并设成 Sprite（PPU 100、不压缩、无 mipmap）。</summary>
        private static Sprite EnsureSprite(string path, byte[] png)
        {
            if (!File.Exists(path))
                File.WriteAllBytes(path, png);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>白环：外径 120、内径 108（128 的画布留 4px 边），边缘 1px 抗锯齿。</summary>
        private static byte[] BuildRingPng()
        {
            var texture = new Texture2D(RingSize, RingSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[RingSize * RingSize];

            var center = RingSize * 0.5f;
            var outer = RingSize * 0.5f - RingThickness;
            var inner = outer - RingThickness;

            for (var y = 0; y < RingSize; y++)
            {
                for (var x = 0; x < RingSize; x++)
                {
                    var dx = x + 0.5f - center;
                    var dy = y + 0.5f - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);

                    var alpha = Mathf.Min(
                        Mathf.SmoothStep(0f, 1f, outer - distance + 0.5f),
                        Mathf.SmoothStep(0f, 1f, distance - inner + 0.5f));

                    pixels[y * RingSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            return Encode(texture, pixels);
        }

        /// <summary>中心点：实心小圆，边缘 1px 抗锯齿。</summary>
        private static byte[] BuildDotPng()
        {
            var texture = new Texture2D(DotSize, DotSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[DotSize * DotSize];

            var center = DotSize * 0.5f;
            var radius = DotSize * 0.5f - 3f;

            for (var y = 0; y < DotSize; y++)
            {
                for (var x = 0; x < DotSize; x++)
                {
                    var dx = x + 0.5f - center;
                    var dy = y + 0.5f - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.SmoothStep(0f, 1f, radius - distance + 0.5f);

                    pixels[y * DotSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            return Encode(texture, pixels);
        }

        private static byte[] Encode(Texture2D texture, Color32[] pixels)
        {
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
                Debug.LogWarning("[AimReticleSetup] " + target.GetType().Name + " 上找不到字段 " + property);
                return;
            }

            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
