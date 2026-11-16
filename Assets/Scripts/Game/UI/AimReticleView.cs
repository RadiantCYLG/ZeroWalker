using UnityEngine;
using ZeroWalker.Framework.Content;
using ZeroWalker.Player;

namespace ZeroWalker.UI
{
    /// <summary>
    /// 瞄准准星：按住瞄准键时，把一个白色圆环 + 中心圆点跟着鼠标走。
    ///
    /// 只做「显示」：位置来自 PlayerInputReader.AimScreenPosition，尺寸来自 ui.json。
    /// 射击方向不在这里算，由 PlayerController 把同一个屏幕点反投影成世界方向。
    /// </summary>
    public sealed class AimReticleView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform ring;
        [SerializeField] private RectTransform dot;
        [SerializeField] private Canvas canvas;
        [SerializeField] private PlayerInputReader input;

        private void Awake()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            if (input == null)
            {
                input = FindObjectOfType<PlayerInputReader>();
            }

            if (root == null)
            {
                root = transform as RectTransform;
            }
        }

        private void OnEnable()
        {
            SetVisible(false);
        }

        private void Update()
        {
            if (input == null || root == null)
            {
                return;
            }

            var aiming = input.AimHeld;
            if (!aiming)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            ApplySize();
            FollowPointer(input.AimScreenPosition);
        }

        private void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible)
            {
                root.gameObject.SetActive(visible);
            }
        }

        private void ApplySize()
        {
            var tuning = ContentLoader.Current.ui;
            SetSize(ring, tuning.aimRingSize);
            SetSize(dot, tuning.aimDotSize);
        }

        private static void SetSize(RectTransform target, float size)
        {
            if (target == null)
            {
                return;
            }

            var current = target.sizeDelta;
            if (!Mathf.Approximately(current.x, size) || !Mathf.Approximately(current.y, size))
            {
                target.sizeDelta = new Vector2(size, size);
            }
        }

        /// <summary>屏幕坐标 → 父 RectTransform 的本地坐标，Overlay 与相机模式都能用。</summary>
        private void FollowPointer(Vector2 screenPosition)
        {
            var parent = root.parent as RectTransform;
            if (parent == null)
            {
                root.position = new Vector3(screenPosition.x, screenPosition.y, 0f);
                return;
            }

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
