using UnityEngine;
using UnityEngine.UI;
using ZeroWalker.Framework.Content;
using ZeroWalker.Player;

namespace ZeroWalker.UI
{
    /// <summary>
    /// 瞄准准星：按住瞄准键（默认鼠标右键）时，把圆环准星摆到鼠标位置。
    ///
    /// 本组件要挂在一个**常驻激活**的对象上（菜单会挂在 HUD 画布上），它控制子对象 <see cref="root"/> 的显隐。
    /// 早期版本把组件挂在准星自身、并用 SetActive(false) 收起来——对象一旦被关掉 Update 就不再执行，
    /// 于是永远等不到「显示」的那一帧，准星再也出不来。
    /// 现在改成：组件常驻，显隐只切两张 Image 的 enabled，并保证 root 对象本身是激活的。
    /// </summary>
    [DisallowMultipleComponent]
    public class AimReticleView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image ring;
        [SerializeField] private Image dot;
        [SerializeField] private Canvas canvas;
        [SerializeField] private PlayerInputReader input;

        private bool _visible;
        private bool _resolved;

        private void Awake()
        {
            Resolve();
        }

        private void OnEnable()
        {
            Resolve();
            SetVisible(false);
        }

        private void Resolve()
        {
            if (input == null) input = FindObjectOfType<PlayerInputReader>();
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            if (root == null) root = transform as RectTransform;
            if (ring == null && root != null) ring = root.GetComponent<Image>();
            if (dot == null && root != null && root.childCount > 0) dot = root.GetChild(0).GetComponent<Image>();

            // 准星对象必须保持激活，否则它自己的 Image 不会被重建；
            // 显隐交给 ring/dot 的 enabled，避免「关掉自己就再也开不回来」。
            if (root != null && !root.gameObject.activeSelf) root.gameObject.SetActive(true);

            _resolved = root != null && ring != null;
        }

        private void Update()
        {
            if (!_resolved) Resolve();
            if (!_resolved) return;

            var aiming = input != null && input.AimHeld;
            SetVisible(aiming);
            if (!aiming) return;

            ApplySize();
            FollowPointer(input.AimScreenPosition);
        }

        private void SetVisible(bool visible)
        {
            if (_visible == visible) return;
            _visible = visible;

            if (ring != null) ring.enabled = visible;
            if (dot != null) dot.enabled = visible;
        }

        /// <summary>按 ui.json 的尺寸设置外圈与中心点（像素，按画布参考分辨率）。</summary>
        private void ApplySize()
        {
            var tuning = ContentLoader.Current.ui;
            if (ring != null) ring.rectTransform.sizeDelta = new Vector2(tuning.aimRingSize, tuning.aimRingSize);
            if (dot != null) dot.rectTransform.sizeDelta = new Vector2(tuning.aimDotSize, tuning.aimDotSize);
        }

        /// <summary>把准星摆到屏幕点对应的画布坐标。</summary>
        private void FollowPointer(Vector2 screenPosition)
        {
            var parent = root.parent as RectTransform;
            if (parent == null) return;

            // Overlay 画布传 null 相机；用 RectTransformUtility 而不是直接写 root.position，
            // 这样在任意 CanvasScaler 设置与锚点下都成立。
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector2 local;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, camera, out local))
            {
                root.anchoredPosition = local;
            }
        }
    }
}
